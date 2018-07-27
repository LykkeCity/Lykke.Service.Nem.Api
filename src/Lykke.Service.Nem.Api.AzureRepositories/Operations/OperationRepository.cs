using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Service.Nem.Api.Domain.Operations;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Nem.Api.AzureRepositories.Operations
{
    public class OperationRepository : IOperationRepository
    {
        private readonly INoSQLTableStorage<OperationEntity> _operationStorage;
        private readonly INoSQLTableStorage<OperationByTxIdEntity> _operationByTxIdStorage;
        private readonly INoSQLTableStorage<TableEntity> _operationByExpiryTimeStorage;

        public OperationRepository(IReloadingManager<string> connectionStringManager, ILogFactory logFactory)
        {
            _operationStorage = AzureTableStorage<OperationEntity>.Create(connectionStringManager, "NemOperations", logFactory);
            _operationByTxIdStorage = AzureTableStorage<OperationByTxIdEntity>.Create(connectionStringManager, "NemOperationsByTxId", logFactory);
            _operationByExpiryTimeStorage = AzureTableStorage<TableEntity>.Create(connectionStringManager, "NemOperationsByExpiryTime", logFactory);
        }

        public async Task Upsert(Guid operationId, string fromAddress, string toAddress, bool includeFee,
            ulong amountInBaseUnit, decimal amount, ulong feeInBaseUnit, decimal fee, string assetId)
        {
            await _operationStorage.InsertOrMergeAsync(new OperationEntity
            {
                PartitionKey = operationId.ToString(),
                RowKey = string.Empty,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Amount = amount,
                AmountInBaseUnit = amountInBaseUnit,
                Fee = fee,
                FeeInBaseUnit = feeInBaseUnit,
                IncludeFee = includeFee,
                AssetId = assetId,
                BuildTime = DateTime.UtcNow
            });
        }

        public async Task Update(Guid operationId,
            DateTime? sendTime = null, DateTime? expiryTime = null, DateTime? completionTime = null, DateTime? blockTime = null, DateTime? failTime = null, DateTime? deleteTime = null,
            string txId = null, ulong? block = null, string error = null)
        {
            if (txId != null)
            {
                await _operationByTxIdStorage.InsertOrMergeAsync(new OperationByTxIdEntity
                {
                    PartitionKey = txId,
                    RowKey = string.Empty,
                    OperationId = operationId
                });
            }

            if (expiryTime != null)
            {
                await _operationByExpiryTimeStorage.InsertOrMergeAsync(new TableEntity
                {
                    PartitionKey = expiryTime.Value.ToUniversalTime().ToString("O"),
                    RowKey = operationId.ToString(),
                });
            }

            await _operationStorage.MergeAsync(operationId.ToString(), string.Empty, op =>
            {
                op.SendTime = sendTime ?? op.SendTime;
                op.ExpiryTime = expiryTime ?? op.ExpiryTime;
                op.CompletionTime = completionTime ?? op.CompletionTime;
                op.BlockTime = blockTime ?? op.BlockTime;
                op.FailTime = failTime ?? op.FailTime;
                op.DeleteTime = deleteTime ?? op.DeleteTime;
                op.TxId = txId ?? op.TxId;
                op.Block = block ?? op.Block;
                op.Error = error ?? op.Error;
                return op;
            });
        }

        public async Task<IOperation> Get(Guid operationId)
        {
            return await _operationStorage.GetDataAsync(operationId.ToString(), string.Empty);
        }

        public async Task<Guid?> GetOperationIdByTxId(string txId)
        {
            var index = await _operationByTxIdStorage.GetDataAsync(txId, string.Empty);
            return index?.OperationId;
        }

        public async Task<Guid[]> GeOperationIdByExpiryTime(DateTime from, DateTime to)
        {
            var filterFrom = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThan, from.ToUniversalTime().ToString("O"));
            var filterTo = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, to.ToUniversalTime().ToString("O"));
            var query = new TableQuery<TableEntity>().Where(TableQuery.CombineFilters(filterFrom, TableOperators.And, filterTo));
            var items = new List<Guid>();

            string continuation = null;

            do
            {
                var chunk = await _operationByExpiryTimeStorage.GetDataWithContinuationTokenAsync(query, continuation);
                continuation = chunk.ContinuationToken;
                items.AddRange(chunk.Entities.Select(e => Guid.Parse(e.RowKey)));
            } while (continuation != null);

            return items.ToArray();
        }
    }
}