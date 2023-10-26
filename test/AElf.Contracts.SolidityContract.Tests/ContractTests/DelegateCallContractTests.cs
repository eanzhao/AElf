using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.ABI.Encoders;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class DelegateCallContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task DelegateCallTest()
    {
        Address delegateeContractAddress, delegatorContractAddress;
        {
            const string solFilePath = "contracts/delegate_call_delegatee.sol";
            var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
            delegateeContractAddress = executionResult.Output;
        }
        {
            const string solFilePath = "contracts/delegate_call_delegator.sol";
            var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
            delegatorContractAddress = executionResult.Output;
        }

        var address = new AddressTypeEncoder().Encode(delegateeContractAddress.AElfAddressToEthAddress()).ToHex();
        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
            new ABIValue("address", address),
            new ABIValue("uint256", 1616)));
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "setVars",
                input);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Checks
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "num");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}