using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.AddSubscriptionPayment;

public class AddSubscriptionPaymentCommand : IRequest<bool>
{
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public bool PayFromWallet { get; set; }
}
