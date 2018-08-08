using Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums;
using ProtoBuf;

namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Events
{
    [ProtoContract]
    public class FeeChargedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public string OperationId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public FeeOperationType OperationType { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public string Fee { get; set; }

    }
}
