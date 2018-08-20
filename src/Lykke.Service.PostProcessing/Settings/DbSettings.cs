using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PostProcessing.Settings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
