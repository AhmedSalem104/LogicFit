using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateMealItem;

public class UpdateMealItemCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public int? FoodId { get; set; }
    public double AssignedQuantity { get; set; }
}
