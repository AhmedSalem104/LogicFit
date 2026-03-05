using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;

public class CreateClientSubscriptionCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
    public bool PayFromWallet { get; set; }
}
