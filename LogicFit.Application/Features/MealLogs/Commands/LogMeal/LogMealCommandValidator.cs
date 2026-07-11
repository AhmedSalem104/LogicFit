using FluentValidation;

namespace LogicFit.Application.Features.MealLogs.Commands.LogMeal;

public class LogMealCommandValidator : AbstractValidator<LogMealCommand>
{
    public LogMealCommandValidator()
    {
        RuleFor(x => x.MealItemId).NotEmpty();
        RuleFor(x => x.ConsumedQuantity)
            .GreaterThan(0).WithMessage("Consumed quantity must be greater than zero.");
    }
}
