using FluentValidation;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateMealItem;

public class UpdateMealItemCommandValidator : AbstractValidator<UpdateMealItemCommand>
{
    public UpdateMealItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Item Id is required");

        RuleFor(x => x.AssignedQuantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}
