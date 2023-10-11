using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.ABI.Decoders;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class CallerIsOriginTest: SolidityContractTestBase
{
    //TODO This is a WIP and needs further study
    // [Fact] public async Task<Address> CallerIsOrigin()
    // {
    //     const string solFilePath = "contracts/caller_is_origin.sol";
    //     var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
    //     var contractAddress = executionResult.Output;
    //     var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "caller_is_origin");
    //     var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
    //     txResult.Status.ShouldBe(TransactionResultStatus.Mined);
    //     var decoder = new IntTypeDecoder();
    //     var result = decoder.DecodeLong(txResult.ReturnValue.Reverse().ToArray());
    //     result.ShouldBe(1);
    //     return contractAddress;
    // }
}