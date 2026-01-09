using MediatR;

namespace LogicFit.Application.Features.Muscles.Commands.DeleteMuscle;

public class DeleteMuscleCommand : IRequest<bool>
{
    public int Id { get; set; }
}
