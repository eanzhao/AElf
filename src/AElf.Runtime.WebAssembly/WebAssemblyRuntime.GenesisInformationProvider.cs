using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Runtime.WebAssembly;

internal class GenesisInformationProvider
{

    private readonly IConsensusReaderContextService _consensusReaderContextService;
    private readonly IContractReaderFactory<BasicContractZeroImplContainer.BasicContractZeroImplStub> _contractReaderFactory;

    public GenesisInformationProvider(
        IContractReaderFactory<BasicContractZeroImplContainer.BasicContractZeroImplStub> contractReaderFactory,
        IConsensusReaderContextService consensusReaderContextService)
    {
        _contractReaderFactory = contractReaderFactory;
        _consensusReaderContextService = consensusReaderContextService;
    }

    public async Task<bool> GetContractExistAsync(ChainContext chainContext, Address address)
    {
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var contractExist =
            await _contractReaderFactory.Create(contractReaderContext).GetContractHash.CallAsync(address);
        return contractExist != null;
    }

}