using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Lykke.Service.Nem.Api.Domain.Assets;

namespace Lykke.Service.Nem.Api.Models.Assets
{
    public class CreateAssetRequest : IAsset
    {
        [Required]
        public string AssetId { get; set; }

        public string Address { get; set; }

        public string Name { get; set; }

        [Range(0, 28)]
        public int Accuracy { get; set; }
    }
}
