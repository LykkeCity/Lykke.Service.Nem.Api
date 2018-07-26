using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Nem.Api.Domain.Assets;
using Lykke.SettingsReader;

namespace Lykke.Service.Nem.Api.AzureRepositories.Assets
{
    public class AssetRepository : IAssetRepository
    {
        private readonly INoSQLTableStorage<AssetEntity> _storage;

        public AssetRepository(IReloadingManager<string> connectionStringManager, ILogFactory logFactory)
        {
            _storage = AzureTableStorage<AssetEntity>.Create(connectionStringManager, "NemAssets", logFactory);
        }

        public async Task Upsert(IAsset asset)
        {
            await _storage.InsertOrMergeAsync(new AssetEntity
            {
                PartitionKey = asset.AssetId,
                RowKey = string.Empty,
                Address = asset.Address,
                Name = asset.Name,
                Accuracy = asset.Accuracy
            });
        }

        public async Task<(string continuation, IAsset[] items)> Get(int take = 100, string continuation = null)
        {
            var chunk = await _storage.GetDataWithContinuationTokenAsync(take, continuation);

            return (
                chunk.ContinuationToken,
                chunk.Entities.ToArray()
            );
        }

        public async Task<IAsset> Get(string assetId)
        {
            return await _storage.GetDataAsync(assetId, string.Empty);
        }
    }
}
