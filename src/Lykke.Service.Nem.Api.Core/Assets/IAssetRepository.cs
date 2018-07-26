using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain.Assets
{
    public interface IAssetRepository : IRepository<IAsset>
    {
        Task<IAsset> Get(string assetId);
        Task Upsert(IAsset asset);
    }
}
