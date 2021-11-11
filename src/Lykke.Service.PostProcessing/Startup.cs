using JetBrains.Annotations;
using Lykke.Service.PostProcessing.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using Antares.Sdk;
using Autofac;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;

namespace Lykke.Service.PostProcessing
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "PostProcessing API",
            ApiVersion = "v1"
        };

        private LykkeServiceOptions<AppSettings> _lykkeOptions;
        private IReloadingManagerWithConfiguration<AppSettings> _settings;

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            (_lykkeOptions, _settings) = services.ConfigureServices<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "PostProcessingLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.PostProcessingService.Db.LogsConnString;
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
        
        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var configurationRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.ConfigureContainerBuilder(_lykkeOptions, configurationRoot, _settings);
        }
    }
}
