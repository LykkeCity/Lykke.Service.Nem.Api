using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Nem.Api.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NemApiSettings
    {
        public DbSettings Db { get; set; }
        public BlockchainSettings Blockchain { get; set; }
    }
}
