namespace Lykke.Service.Nem.Api.Models
{
    public class TransactionContext
    {
        public int ExpiresInMinutes { get; set; }
        public string To { get; set; }
        public string AssetId { get; set; }
        public ulong Amount { get; set; }
    }
}
