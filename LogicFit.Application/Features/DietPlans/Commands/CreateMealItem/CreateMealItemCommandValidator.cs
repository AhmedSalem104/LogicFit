using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateMealItem;

public sealed class CreateMealItemCommandValidator : AbstractValidator<CreateMealItemCommand>
{
    public CreateMealItemCommandValidator()
    {
        RuleFor(x => x.MealId).NotEmpty();
        RuleFor(x => x.FoodId).GreaterThan(0);
        RuleFor(x => x.AssignedQuantity).GreaterThan(0);
    }
}
