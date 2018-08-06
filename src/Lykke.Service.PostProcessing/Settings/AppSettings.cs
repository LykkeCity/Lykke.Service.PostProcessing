using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public PostProcessingSettings PostProcessingService { get; set; }
    }
}
