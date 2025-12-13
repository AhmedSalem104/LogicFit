using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateWorkoutProgram;

public class UpdateWorkoutProgramCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
