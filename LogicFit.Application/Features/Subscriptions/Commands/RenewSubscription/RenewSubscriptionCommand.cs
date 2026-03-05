using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.RenewSubscription;

public class RenewSubscriptionCommand : IRequest<Guid>
{
    public Guid SubscriptionId { get; set; }
    public Guid? PlanId { get; set; }
    public DateTime? StartDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
    public bool PayFromWallet { get; set; }
}
