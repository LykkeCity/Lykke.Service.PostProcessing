using System.Collections.Generic;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Models;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    /// <summary>
    /// Exectuion processed event
    /// </summary>
    [ProtoContract]
    public class ExecutionProcessedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public IReadOnlyList<OrderModel> Orders { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public long SequenceNumber { get; set; }
    }
}
