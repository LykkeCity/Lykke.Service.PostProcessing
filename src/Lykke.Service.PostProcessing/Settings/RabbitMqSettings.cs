using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        [AmqpCheck]
        [Optional]
        public string AlternativeConnectionString { get; set; }
        public string Exchange { get; set; }
    }
}
