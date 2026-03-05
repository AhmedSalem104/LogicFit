using FluentValidation;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;

public class CreateClientSubscriptionCommandValidator : AbstractValidator<CreateClientSubscriptionCommand>
{
    public CreateClientSubscriptionCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan ID is required");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative")
            .When(x => x.Discount.HasValue);

        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("Amount paid cannot be negative")
            .When(x => x.AmountPaid.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => x.Notes != null);
    }
}
