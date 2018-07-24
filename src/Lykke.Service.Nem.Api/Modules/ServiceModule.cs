using Autofac;
using Lykke.Sdk;
using Lykke.Service.Nem.Api.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Nem.Api.Modules
{    
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_appSettings.CurrentValue.NemApi.Blockchain);
        }
    }
}
