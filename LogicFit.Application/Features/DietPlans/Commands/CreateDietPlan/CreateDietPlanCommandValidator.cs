using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;

public sealed class CreateDietPlanCommandValidator : AbstractValidator<CreateDietPlanCommand>
{
    public CreateDietPlanCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.MealsPerDay).GreaterThan(0).When(x => x.MealsPerDay.HasValue);
        RuleFor(x => x.TargetCalories).GreaterThan(0);
        RuleFor(x => x.TargetProtein).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetCarbs).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetFats).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Meals).NotEmpty().When(x => !x.Status.HasValue || x.Status != LogicFit.Domain.Enums.PlanStatus.Draft)
            .WithMessage("At least one meal is required for an active diet plan.");

        RuleForEach(x => x.Meals).ChildRules(meal =>
        {
            meal.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            meal.RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
            meal.RuleFor(x => x.Items).NotEmpty().WithMessage("Each meal needs a food item.");
            meal.RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.FoodId).GreaterThan(0);
                item.RuleFor(x => x.AssignedQuantity).GreaterThan(0);
            });
        });
    }
}
