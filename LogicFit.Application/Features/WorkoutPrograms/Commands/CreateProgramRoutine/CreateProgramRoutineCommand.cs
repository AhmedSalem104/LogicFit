using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateProgramRoutine;

public class CreateProgramRoutineCommand : IRequest<Guid>
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
}
