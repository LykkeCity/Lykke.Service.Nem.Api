using Autofac;
using Lykke.Sdk;
using Lykke.Service.Nem.Api.AzureRepositories.Assets;
using Lykke.Service.Nem.Api.AzureRepositories.Operations;
using Lykke.Service.Nem.Api.Domain.Assets;
using Lykke.Service.Nem.Api.Domain.Balances;
using Lykke.Service.Nem.Api.Domain.Operations;
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

            builder.RegisterType<AssetRepository>()
                .As<IAssetRepository>()
                .WithParameter(TypedParameter.From(_appSettings.ConnectionString(s => s.NemApi.Db.DataConnString)))
                .SingleInstance();

            builder.RegisterType<BalanceAddressRepository>()
                .As<IBalanceAddressRepository>()
                .WithParameter(TypedParameter.From(_appSettings.ConnectionString(s => s.NemApi.Db.DataConnString)))
                .SingleInstance();

            builder.RegisterType<OperationRepository>()
                .As<IOperationRepository>()
                .WithParameter(TypedParameter.From(_appSettings.ConnectionString(s => s.NemApi.Db.DataConnString)))
                .SingleInstance();
        }
    }
}
