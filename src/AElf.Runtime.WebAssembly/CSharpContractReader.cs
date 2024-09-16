using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

internal class CSharpContractReader : ICSharpContractReader, ISingletonDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly ISmartContractAddressProvider _smartContractAddressProvider;
    private readonly IContractReaderFactory<TokenContractContainer.TokenContractStub> _tokenContractReaderFactory;

    public CSharpContractReader(IBlockchainService blockchainService,
        ISmartContractAddressProvider smartContractAddressProvider,
        IContractReaderFactory<TokenContractContainer.TokenContractStub> tokenContractReaderFactory)
    {
        _blockchainService = blockchainService;
        _smartContractAddressProvider = smartContractAddressProvider;
        _tokenContractReaderFactory = tokenContractReaderFactory;
    }

    public async Task<long> GetBalanceAsync(Address from, Address owner, string? symbol = null)
    {
        var chain = await _blockchainService.GetChainAsync();
        var tokenContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            },
            TokenSmartContractAddressNameProvider.StringName);
        var tokenContractStub = _tokenContractReaderFactory.Create(new ContractReaderContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight,
            ContractAddress = tokenContractAddress.Address
        });
        var querySymbol = symbol ?? (await tokenContractStub.GetNativeTokenInfo.CallAsync(new Empty())).Symbol;
        var output = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = querySymbol,
            Owner = owner
        });
        return output.Balance;
    }
}