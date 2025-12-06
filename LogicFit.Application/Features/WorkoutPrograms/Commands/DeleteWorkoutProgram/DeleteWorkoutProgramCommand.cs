using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteWorkoutProgram;

public class DeleteWorkoutProgramCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
