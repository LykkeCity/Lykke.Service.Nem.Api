using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Service.Nem.Api.AzureRepositories.Assets;
using Lykke.Service.Nem.Api.Domain.Balances;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

public class BalanceAddressRepository : IBalanceAddressRepository
{
    private readonly INoSQLTableStorage<TableEntity> _storage;

    public BalanceAddressRepository(IReloadingManager<string> connectionStringManager, ILogFactory logFactory)
    {
        _storage = AzureTableStorage<TableEntity>.Create(connectionStringManager, "NemBalanceAddresses", logFactory);
    }

    public async Task<bool> Exists(string address)
    {
        return await _storage.RecordExistsAsync(new TableEntity(address, string.Empty));
    }

    public async Task Upsert(string address)
    {
        await _storage.InsertOrMergeAsync(new TableEntity(address, string.Empty));
    }

    public async Task Delete(string address)
    {
        await _storage.DeleteAsync(address, string.Empty);
    }

    public async Task<(string continuation, string[] items)> Get(int take = 100, string continuation = null)
    {
        var chunk = await _storage.GetDataWithContinuationTokenAsync(take, continuation);

        return (
            chunk.ContinuationToken,
            chunk.Entities.Select(e => e.PartitionKey).ToArray()
        );
    }
}