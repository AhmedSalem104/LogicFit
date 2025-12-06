using MediatR;

namespace LogicFit.Application.Features.Foods.Commands.DeleteFood;

public class DeleteFoodCommand : IRequest<bool>
{
    public int Id { get; set; }
}
