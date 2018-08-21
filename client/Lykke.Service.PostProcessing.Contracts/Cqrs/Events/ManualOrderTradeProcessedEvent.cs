using Lykke.Service.PostProcessing.Contracts.Cqrs.Models;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    /// <summary>
    /// An order that was placed manually, partially or completely executed.
    /// </summary>
    [ProtoContract]
    public class ManualOrderTradeProcessedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public OrderModel Order { get; set; }
    }
}
