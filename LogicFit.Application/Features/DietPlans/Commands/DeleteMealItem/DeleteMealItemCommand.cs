using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteMealItem;

public class DeleteMealItemCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
