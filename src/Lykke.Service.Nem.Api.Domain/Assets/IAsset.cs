using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Nem.Api.Domain.Assets
{
    public interface IAsset
    {
        string AssetId { get; }
        string Address { get; }
        string Name { get; }
        int Accuracy { get; }
    }
}
