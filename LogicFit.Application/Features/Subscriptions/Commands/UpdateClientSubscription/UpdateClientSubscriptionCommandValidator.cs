using FluentValidation;

namespace LogicFit.Application.Features.Subscriptions.Commands.UpdateClientSubscription;

public class UpdateClientSubscriptionCommandValidator : AbstractValidator<UpdateClientSubscriptionCommand>
{
    public UpdateClientSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Subscription ID is required");

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
