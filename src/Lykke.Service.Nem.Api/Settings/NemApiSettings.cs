using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using Lykke.Common.Chaos;
using System;

namespace Lykke.Service.Nem.Api.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NemApiSettings
    {
        [Optional]
        public string ExplorerUrl { get; set; }
        public string NemUrl { get; set; }
        public int RequiredConfirmations { get; set; }
        public TimeSpan ExpiresIn { get; set; }
        public DbSettings Db { get; set; }
        public ChaosSettings ChaosKitty { get; set; }
    }
}
