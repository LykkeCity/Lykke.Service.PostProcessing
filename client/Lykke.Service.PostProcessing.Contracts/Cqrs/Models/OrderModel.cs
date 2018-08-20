using System;
using System.Collections.Generic;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Models
{
    /// <summary>
    /// Executed order
    /// </summary>
    [ProtoContract]
    public class OrderModel
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid Id { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public Guid MatchingId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public Guid WalletId { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public OrderType Type { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public OrderSide Side { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public OrderStatus Status { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public string AssetPairId { get; set; }

        [ProtoMember(8, IsRequired = true)]
        public decimal Volume { get; set; }

        [ProtoMember(9, IsRequired = false)]
        public decimal? Price { get; set; }

        [ProtoMember(10, IsRequired = true)]
        public DateTime CreateDt { get; set; }

        [ProtoMember(11, IsRequired = true)]
        public DateTime RegisterDt { get; set; }

        [ProtoMember(12, IsRequired = true)]
        public DateTime StatusDt { get; set; }

        [ProtoMember(13, IsRequired = false)]
        public DateTime? MatchDt { get; set; }

        [ProtoMember(14, IsRequired = false)]
        public decimal? RemainingVolume { get; set; }

        [ProtoMember(15, IsRequired = true)]
        public string RejectReason { get; set; }

        [ProtoMember(16, IsRequired = false)]
        public decimal? LowerLimitPrice { get; set; }

        [ProtoMember(17, IsRequired = false)]
        public decimal? LowerPrice { get; set; }

        [ProtoMember(18, IsRequired = false)]
        public decimal? UpperLimitPrice { get; set; }

        [ProtoMember(19, IsRequired = false)]
        public decimal? UpperPrice { get; set; }

        [ProtoMember(20, IsRequired = true)]
        public bool Straight { get; set; } = true;

        [ProtoMember(21, IsRequired = false)]
        public IReadOnlyCollection<TradeModel> Trades { get; set; } = new List<TradeModel>();
    }
}
