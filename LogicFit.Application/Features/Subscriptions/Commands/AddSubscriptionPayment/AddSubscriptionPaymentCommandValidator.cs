using FluentValidation;

namespace LogicFit.Application.Features.Subscriptions.Commands.AddSubscriptionPayment;

public class AddSubscriptionPaymentCommandValidator : AbstractValidator<AddSubscriptionPaymentCommand>
{
    public AddSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Subscription ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method");
    }
}
