using Autofac;
using Lykke.Sdk;
using Lykke.Service.PostProcessing.RabbitSubscribers;
using Lykke.Service.PostProcessing.Settings;
using Lykke.SettingsReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Lykke.Service.PostProcessing.Modules
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
            builder.RegisterType<MeRabbitSubscriber>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PostProcessingService.MatchingEngineRabbit));

            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                return settings;
            });

            builder.RegisterType<StartupManager>().As<IStartupManager>();
        }
    }
}
