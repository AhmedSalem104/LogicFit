using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateMealItem;

public class CreateMealItemCommand : IRequest<Guid>
{
    public Guid MealId { get; set; }
    public int FoodId { get; set; }
    public double AssignedQuantity { get; set; }
}
