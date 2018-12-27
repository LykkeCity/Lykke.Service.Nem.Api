using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Nem.Api.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class NemApiSettings
    {
        [Optional]
        public string     ExplorerUrl           { get; set; }
        public string     NemUrl                { get; set; }
        public int        RequiredConfirmations { get; set; }
        public int        ExpiresInMinutes      { get; set; }
        public DbSettings Db                    { get; set; }
    }
}
