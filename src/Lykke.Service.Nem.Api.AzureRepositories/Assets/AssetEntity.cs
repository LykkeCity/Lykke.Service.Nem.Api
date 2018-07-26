using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Nem.Api.Domain.Assets;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Nem.Api.AzureRepositories.Assets
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class AssetEntity : AzureTableEntity, IAsset
    {
        [IgnoreProperty]
        public string AssetId { get => PartitionKey; }
        public string Address { get; set; }
        public string Name { get; set; }
        public int Accuracy { get; set; }
    }
}
