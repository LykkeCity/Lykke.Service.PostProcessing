using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PostProcessing.Settings
{
    public class CqrsSettings
    {
        [AmqpCheck]
        public string RabbitConnString { get; set; }
    }
}
