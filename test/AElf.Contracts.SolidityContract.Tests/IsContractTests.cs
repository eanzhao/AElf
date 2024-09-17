using System.IO;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class IsContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> isContractTest()
    {
        const string solFilePath = "contracts/is_contract.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("address", contractAddress));
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "contract_oracle", ByteString.CopyFrom(parameter));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }
    
}