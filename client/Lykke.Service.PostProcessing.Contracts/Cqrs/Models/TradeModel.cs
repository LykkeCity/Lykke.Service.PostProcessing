using System;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Models
{
    /// <summary>
    /// User trade item
    /// </summary>
    [ProtoContract]
    public class TradeModel
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid Id { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public Guid WalletId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public string AssetPairId { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public string AssetId { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public decimal Volume { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public decimal Price { get; set; }

        [ProtoMember(7, IsRequired = true)]
        public DateTime Timestamp { get; set; }

        [ProtoMember(8, IsRequired = true)]
        public string OppositeAssetId { get; set; }

        [ProtoMember(9, IsRequired = true)]
        public decimal OppositeVolume { get; set; }

        [ProtoMember(10, IsRequired = true)]
        public int Index { get; set; }

        [ProtoMember(11, IsRequired = true)]
        public TradeRole Role { get; set; }

        [ProtoMember(12, IsRequired = false)]
        public decimal? FeeSize { get; set; }

        [ProtoMember(13, IsRequired = false)]
        public string FeeAssetId { get; set; }
    }
}
