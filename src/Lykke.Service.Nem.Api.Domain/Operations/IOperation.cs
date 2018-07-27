using System;

namespace Lykke.Service.Nem.Api.Domain.Operations
{
    public interface IOperation
    {
        Guid OperationId { get; }
        DateTime BuildTime { get; }
        DateTime? ExpiryTime { get; }
        DateTime? SendTime { get; }
        DateTime? BlockTime { get; }
        DateTime? CompletionTime { get; }
        DateTime? FailTime { get; }
        DateTime? DeleteTime { get; }
        ulong? Block { get; }
        string TxId { get; }
        string Error { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string AssetId { get; }
        decimal Amount { get; }
        ulong AmountInBaseUnit { get; }
        decimal Fee { get; }
        ulong FeeInBaseUnit { get; }
        bool IncludeFee { get; }
    }
}