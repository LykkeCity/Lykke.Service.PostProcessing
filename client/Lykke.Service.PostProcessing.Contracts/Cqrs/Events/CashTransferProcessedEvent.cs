using System;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    /// <summary>
    /// Cash transfer processed event
    /// </summary>
    [ProtoContract]
    public class CashTransferProcessedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid Id { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public Guid FromWalletId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public Guid ToWalletId { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public decimal Volume { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public string AssetId { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public DateTime Timestamp { get; set; }

        [ProtoMember(7, IsRequired = false)]
        public Guid? FeeSourceWalletId { get; set; }

        [ProtoMember(8, IsRequired = false)]
        public decimal? FeeSize { get; set; }
    }
}
