using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Nem.Api.Settings
{
    public class BlockchainSettings
    {
        public string Network { get; set; }
        public string Host { get; set; }
        public int ExpiresInMinutes { get; set; }
    }
}
