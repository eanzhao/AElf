using AElf.Types;

namespace AElf.Kernel.TransactionPool
{
    public class TransactionValidationStatusChangedEvent
    {
        public Hash TransactionId { get; set; }
        public TransactionResultStatus TransactionResultStatus { get; set; }
        public string Error { get; set; }
    }
}