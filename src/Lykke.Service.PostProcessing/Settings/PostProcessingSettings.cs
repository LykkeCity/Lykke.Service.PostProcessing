using JetBrains.Annotations;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PostProcessingSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings MatchingEngineRabbit { get; set; }

        public CqrsSettings Cqrs { get; set; }
    }
}
