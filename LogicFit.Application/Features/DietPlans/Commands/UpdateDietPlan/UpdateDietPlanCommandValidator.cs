using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDietPlan;

public class UpdateDietPlanCommandValidator : AbstractValidator<UpdateDietPlanCommand>
{
    public UpdateDietPlanCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Plan Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.TargetCalories)
            .GreaterThan(0).WithMessage("Target calories must be greater than 0");

        RuleFor(x => x.TargetProtein)
            .GreaterThanOrEqualTo(0).WithMessage("Target protein must be non-negative");

        RuleFor(x => x.TargetCarbs)
            .GreaterThanOrEqualTo(0).WithMessage("Target carbs must be non-negative");

        RuleFor(x => x.TargetFats)
            .GreaterThanOrEqualTo(0).WithMessage("Target fats must be non-negative");
    }
}
