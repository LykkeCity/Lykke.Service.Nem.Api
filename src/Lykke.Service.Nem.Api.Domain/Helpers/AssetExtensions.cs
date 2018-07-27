using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Nem.Api.Domain.Assets;

namespace Lykke.Service.Nem.Api.Domain.Helpers
{
    public static class AssetExtensions
    {
        public static decimal ToDecimal(this IAsset asset, ulong amountInBaseUnit)
        {
            return amountInBaseUnit / Convert.ToDecimal(Math.Pow(10, asset.Accuracy));
        }
    }
}