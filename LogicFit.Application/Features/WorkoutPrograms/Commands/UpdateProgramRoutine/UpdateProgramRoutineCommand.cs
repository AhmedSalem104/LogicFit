using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateProgramRoutine;

public class UpdateProgramRoutineCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
}
