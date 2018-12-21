using System;
using System.Net.Http;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.BlockchainApi.Sdk;
using Lykke.Service.Nem.Api.Blockchain;
using Lykke.Service.Nem.Api.Services;
using Lykke.Service.Nem.Api.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Nem.Api
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "NemApi API",
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
                    logs.AzureTableName = "Nem.ApiLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.NemApi.Db.LogsConnString;
                };

                options.Extend = (svcCollection, settings) =>
                {
                    svcCollection
                        .AddHttpClient()
                        .AddBlockchainApi(sp => 
                            new NemApi(
                                settings.CurrentValue.NemApi.NemUrl,
                                settings.CurrentValue.NemApi.ExplorerUrl,
                                settings.CurrentValue.NemApi.RequiredConfirmations,
                                settings.CurrentValue.NemApi.ExpiresInMinutes,
                                settings.CurrentValue.NemApi.Network,
                                sp.GetRequiredService<INemClient>()));
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
