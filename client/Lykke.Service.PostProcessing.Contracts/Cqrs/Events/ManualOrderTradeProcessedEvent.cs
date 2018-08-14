using Lykke.Service.PostProcessing.Contracts.Cqrs.Models;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    [ProtoContract]
    public class ManualOrderTradeProcessedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public OrderModel Order { get; set; }
    }
}
