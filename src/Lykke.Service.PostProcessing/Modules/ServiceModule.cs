using Antares.Sdk.Services;
using Autofac;
using Lykke.Mailerlite.ApiClient;
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
            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<MeRabbitSubscriber>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PostProcessingService.MatchingEngineRabbit))
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PostProcessingService.WalletIdsToLog));
            
            builder
                .RegisterInstance(new LykkeMailerliteClient(_appSettings.CurrentValue.MailerliteServiceClient.GrpcServiceUrl))
                .As<ILykkeMailerliteClient>();
            
            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                return settings;
            });
        }
    }
}
