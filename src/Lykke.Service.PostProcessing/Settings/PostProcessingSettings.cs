using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PostProcessing.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PostProcessingSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings MatchingEngineRabbit { get; set; }

        public CqrsSettings Cqrs { get; set; }

        [Optional]
        public IReadOnlyList<string> WalletIdsToLog { get; set; } = Array.Empty<string>();

        [Optional]
        public bool UseDeadletterExchange { get; set; }
    }
}
