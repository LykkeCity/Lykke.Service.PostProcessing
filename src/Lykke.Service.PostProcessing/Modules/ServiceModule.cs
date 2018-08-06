using Autofac;
using Lykke.Service.PostProcessing.RabbitSubscribers;
using Lykke.Service.PostProcessing.Settings;
using Lykke.SettingsReader;

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
                .AutoActivate()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PostProcessingService.MatchingEngineRabbit));

        }
    }
}
