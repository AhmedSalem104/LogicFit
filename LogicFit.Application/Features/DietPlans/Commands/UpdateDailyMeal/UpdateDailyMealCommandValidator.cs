using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDailyMeal;

public class UpdateDailyMealCommandValidator : AbstractValidator<UpdateDailyMealCommand>
{
    public UpdateDailyMealCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Meal Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Order index must be non-negative");
    }
}
