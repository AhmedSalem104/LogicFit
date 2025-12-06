using FluentValidation;

namespace LogicFit.Application.Features.Foods.Commands.CreateFood;

public class CreateFoodCommandValidator : AbstractValidator<CreateFoodCommand>
{
    public CreateFoodCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.CaloriesPer100g)
            .GreaterThanOrEqualTo(0).WithMessage("Calories must be non-negative");

        RuleFor(x => x.ProteinPer100g)
            .GreaterThanOrEqualTo(0).WithMessage("Protein must be non-negative");

        RuleFor(x => x.CarbsPer100g)
            .GreaterThanOrEqualTo(0).WithMessage("Carbs must be non-negative");

        RuleFor(x => x.FatsPer100g)
            .GreaterThanOrEqualTo(0).WithMessage("Fats must be non-negative");
    }
}
