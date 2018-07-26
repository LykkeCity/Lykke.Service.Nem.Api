using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Nem.Api.Domain.Assets;

namespace Lykke.Service.Nem.Api.Helpers
{
    public static class AssetExtensions
    {
        public static AssetResponse ToResponse(this IAsset asset)
        {
            if (asset != null)
            {
                return new AssetResponse
                {
                    AssetId = asset.AssetId,
                    Address = asset.Address,
                    Name = asset.Name,
                    Accuracy = asset.Accuracy
                };
            }

            return null;
        }

        public static AssetContract ToContract(this IAsset asset)
        {
            return asset.ToResponse();
        }
    }
}
