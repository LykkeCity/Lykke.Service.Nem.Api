using System;
using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain.Operations
{
    public interface IOperationRepository
    {
        Task Upsert(Guid operationId, string fromAddress, string toAddress,
            bool includeFee, ulong amountInBaseUnit, decimal amount, ulong feeInBaseUnit, decimal fee, string assetId);

        Task<IOperation> Get(Guid operationId);
        
        Task Update(Guid operationId,
            DateTime? sendTime = null, DateTime? expiryTime = null, DateTime? completionTime = null, DateTime? blockTime = null, DateTime? failTime = null, DateTime? deleteTime = null,
            string txId = null, ulong? block = null, string error = null);
    }
}