using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string Exchange { get; set; }
        public string AlternativeExchange { get; set; }
    }
}
