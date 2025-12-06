using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;

public class CreateWorkoutProgramCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
