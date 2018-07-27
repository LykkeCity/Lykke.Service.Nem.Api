using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain.Assets
{
    public interface IAssetRepository
    {
        Task<IAsset> Get(string assetId);
        Task<(string continuation, IAsset[] items)> Get(int take = 100, string continuation = null);
        Task Upsert(IAsset asset);
    }
}
