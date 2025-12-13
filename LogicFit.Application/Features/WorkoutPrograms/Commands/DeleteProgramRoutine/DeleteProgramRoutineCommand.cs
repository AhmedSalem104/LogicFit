using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteProgramRoutine;

public class DeleteProgramRoutineCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
