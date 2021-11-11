using Antares.Sdk.Settings;
using JetBrains.Annotations;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public PostProcessingSettings PostProcessingService { get; set; }
        
        public LykkeMailerliteServiceSettings MailerliteServiceClient { set; get; }
    }
}
