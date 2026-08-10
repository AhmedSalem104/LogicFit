using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDailyMeal;

public sealed class CreateDailyMealCommandValidator : AbstractValidator<CreateDailyMealCommand>
{
    public CreateDailyMealCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
