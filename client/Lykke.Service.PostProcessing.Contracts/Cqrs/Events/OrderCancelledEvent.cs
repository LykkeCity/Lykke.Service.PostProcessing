using Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums;
using ProtoBuf;
using System;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    /// <summary>
    /// An order was cancelled.
    /// </summary>
    [ProtoContract]
    public class OrderCancelledEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid Id { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public Guid WalletId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public OrderStatus Status { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public string AssetPairId { get; set; }

        [ProtoMember(5, IsRequired = false)]
        public decimal? Price { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public decimal Volume { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public DateTime Timestamp { get; set; }
    }
}
