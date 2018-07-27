using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Nem.Api.Domain.Operations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Nem.Api.AzureRepositories.Operations
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class OperationEntity : AzureTableEntity, IOperation
    {
        [IgnoreProperty]
        public Guid OperationId { get => Guid.Parse(PartitionKey); }
        public DateTime BuildTime { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public DateTime? SendTime { get; set; }
        public DateTime? BlockTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public DateTime? FailTime { get; set; }
        public DateTime? DeleteTime { get; set; }
        public ulong? Block { get; set; }
        public string TxId { get; set; }
        public string Error { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public ulong AmountInBaseUnit { get; set; }
        public decimal Fee { get; set; }
        public ulong FeeInBaseUnit { get; set; }
        public bool IncludeFee { get; set; }
    }
}