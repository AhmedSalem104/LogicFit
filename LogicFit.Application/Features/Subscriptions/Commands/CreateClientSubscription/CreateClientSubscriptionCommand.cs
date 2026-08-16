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

    // The onboarding workflow already owns an outer transaction. Keep this
    // orchestration flag internal so public API callers always get an atomic
    // transaction managed by this handler, while onboarding can join its
    // existing transaction instead of opening a nested transaction.
    internal bool UseExistingTransaction { get; set; }
}
