﻿using System.Text.Json;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly.Contract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;
using Solang;
using Solang.Extensions;
using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public class Executive : IExecutive
{
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; }
    public Hash ContractHash { get; set; }
    public Timestamp LastUsedTime { get; set; }
    public string ContractVersion { get; set; }

    private readonly SolangABI _solangAbi;
    private readonly WebAssemblyContractImplementation _webAssemblyContract;
    private readonly WebAssemblySmartContractProxy _smartContractProxy;

    private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
    private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

    public Executive(CompiledContract compiledContract)
    {
        var wasmCode = compiledContract.WasmCode.ToByteArray();
        _solangAbi = JsonSerializer.Deserialize<SolangABI>(compiledContract.Abi)!;
        ContractHash = HashHelper.ComputeFrom(wasmCode);
        _webAssemblyContract = new WebAssemblyContractImplementation(wasmCode);
        _smartContractProxy = new WebAssemblySmartContractProxy(_webAssemblyContract);

        // TODO: Maybe we are able to know the solidity code version.
        ContractVersion = "Unknown solidity version.";
    }

    public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        _hostSmartContractBridgeContext = smartContractBridgeContext;
        _smartContractProxy.InternalInitialize(_hostSmartContractBridgeContext);
        return this;
    }

    public Task ApplyAsync(ITransactionContext transactionContext)
    {
        try
        {
            _hostSmartContractBridgeContext.TransactionContext = transactionContext;
            if (CurrentTransactionContext.CallDepth > CurrentTransactionContext.MaxCallDepth)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                CurrentTransactionContext.Trace.Error = "\n" + "ExceededMaxCallDepth";
                return Task.CompletedTask;
            }

            Execute();
        }
        finally
        {
            _hostSmartContractBridgeContext.TransactionContext = null;
        }

        return Task.CompletedTask;
    }

    private void Execute()
    {
        var startTime = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();
        var methodName = CurrentTransactionContext.Transaction.MethodName;

        try
        {
            var transactionContext = _hostSmartContractBridgeContext.TransactionContext;
            var transaction = transactionContext.Transaction;

            var isCallConstructor = methodName == "deploy" || _solangAbi.GetConstructor() == methodName;

            if (isCallConstructor && _webAssemblyContract.Initialized)
            {
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.Prefailed;
                transactionContext.Trace.Error = "Cannot execute constructor.";
                return;
            }

            var selector = isCallConstructor ? _solangAbi.GetConstructor() : methodName;
            string parameter;
            long value;
            if (isCallConstructor)
            {
                parameter = transaction.Params.ToHex();
                value = 0;
            }
            else
            {
                var parameterWithValue = new TransactionParameterWithValue();
                parameterWithValue.MergeFrom(transaction.Params);
                parameter = parameterWithValue.Parameter.ToHex();
                value = parameterWithValue.Value;
            }

            var action = GetAction(selector, parameter, value, isCallConstructor);
            var invokeResult = new RuntimeActionInvoker().Invoke(action);

            if (!invokeResult.Success)
            {
                _webAssemblyContract.DebugMessages.Add(invokeResult.DebugMessage);
            }

            if (_webAssemblyContract.DebugMessages.Count > 0)
            {
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                transactionContext.Trace.Error = _webAssemblyContract.DebugMessages.First();
            }
            else
            {
                transactionContext.Trace.ReturnValue = ByteString.CopyFrom(_webAssemblyContract.ReturnBuffer);
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
                foreach (var depositedEvent in _webAssemblyContract.Events)
                {
                    transactionContext.Trace.Logs.Add(new LogEvent
                    {
                        Address = transaction.To,
                        Name = depositedEvent.Item1.ToHex(),
                        NonIndexed = ByteString.CopyFrom(depositedEvent.Item2)
                    });
                }
            }

            CurrentTransactionContext.Trace.StateSet = GetChanges();
        }
        catch (Exception ex)
        {
            CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
            CurrentTransactionContext.Trace.Error += ex + "\n";
        }

        var endTime = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
        CurrentTransactionContext.Trace.Elapsed = (endTime - startTime).Ticks;
    }

    private Func<ActionResult> GetAction(string selector, string parameter, long value, bool isCallConstructor)
    {
        _webAssemblyContract.Input = Encoders.Hex.DecodeData(selector + parameter);
        _webAssemblyContract.Value = value;
        var instance = _webAssemblyContract.Instantiate();
        var actionName = isCallConstructor ? "deploy" : "call";
        var action = instance.GetFunction<ActionResult>(actionName);
        if (action is null)
        {
            throw new WebAssemblyRuntimeException($"error: {actionName} export is missing");
        }

        return action;
    }

    private TransactionExecutingStateSet GetChanges()
    {
        var changes = _smartContractProxy.GetChanges();

        var address = _hostSmartContractBridgeContext.Self.ToStorageKey();
        foreach (var key in changes.Writes.Keys)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        foreach (var (key, value) in changes.Deletes)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        foreach (var key in changes.Reads.Keys)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        if (CurrentTransactionContext.Trace.CallStateSet != null)
        {
            changes = changes.Merge(CurrentTransactionContext.Trace.CallStateSet);
        }
        if (CurrentTransactionContext.Trace.DelegateCallStateSet != null)
        {
            changes = changes.Merge(CurrentTransactionContext.Trace.DelegateCallStateSet.ReplaceAddress(address));
        }

        if (!CurrentTransactionContext.Trace.IsSuccessful())
        {
            changes.Writes.Clear();
            changes.Deletes.Clear();
        }

        return changes;
    }

    public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
    {
        throw new NotImplementedException();
    }

    public bool IsView(string methodName)
    {
        throw new NotImplementedException();
    }

    public byte[] GetFileDescriptorSet()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        throw new NotImplementedException();
    }
}