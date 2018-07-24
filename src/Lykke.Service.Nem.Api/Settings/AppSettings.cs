using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.Nem.Api.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public NemApiSettings NemApi { get; set; }
    }
}
