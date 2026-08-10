using MediatR;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateWorkoutProgram;

public class UpdateWorkoutProgramCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? Difficulty { get; set; }
    public int? DaysPerWeek { get; set; }
    public PlanStatus? Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<WorkoutRoutineInputDto>? Routines { get; set; }
}
