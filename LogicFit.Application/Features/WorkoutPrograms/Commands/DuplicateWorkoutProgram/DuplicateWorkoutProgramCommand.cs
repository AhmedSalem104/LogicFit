using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DuplicateWorkoutProgram;

public class DuplicateWorkoutProgramCommand : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid? NewClientId { get; set; }
    public string? NewName { get; set; }
}
