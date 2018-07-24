using io.nem1.sdk.Model.Transactions;

namespace Lykke.Service.Nem.Api.Models
{
    public class SignedTransactionContext
    {
        public string Hash { get; set; }
        public string Payload { get; set; }
        public string Signature { get; set; }
        public string Signer { get; set; }
        public TransactionTypes.Types TransactionType { get; set; }
    }
}
