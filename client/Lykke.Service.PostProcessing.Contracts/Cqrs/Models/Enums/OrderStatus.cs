namespace Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums
{
    public enum OrderStatus
    {
        Unknown,
        Placed,
        PartiallyMatched,
        Matched,
        Pending,
        Cancelled,
        Replaced,
        Rejected
    }
}
