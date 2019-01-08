using System;
using System.Net.Http;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.BlockchainApi.Sdk;
using Lykke.Service.BlockchainApi.Sdk.Domain.DepositWallets;
using Lykke.Service.Nem.Api.Services;
using Lykke.Service.Nem.Api.Settings;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Nem.Api
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "Nem Api",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "NemApiLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.NemApi.Db.LogsConnString;
                };

                options.Extend = (sc, settings) =>
                {
                    sc.AddBlockchainApi(
                        settings.ConnectionString(s => s.NemApi.Db.DataConnString),
                        sp => new NemApi(
                            settings.CurrentValue.NemApi.NemUrl,
                            settings.CurrentValue.NemApi.ExplorerUrl,
                            settings.CurrentValue.NemApi.RequiredConfirmations,
                            settings.CurrentValue.NemApi.ExpiresInMinutes,
                            sp.GetRequiredService<DepositWalletRepository>()
                        ),
                        settings.CurrentValue.NemApi.ChaosKitty
                    );
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }
    }
}
