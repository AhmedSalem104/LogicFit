using FluentValidation;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionFreeze;

public class CreateSubscriptionFreezeCommandValidator : AbstractValidator<CreateSubscriptionFreezeCommand>
{
    public CreateSubscriptionFreezeCommandValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Subscription ID is required");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters")
            .When(x => x.Reason != null);
    }
}
