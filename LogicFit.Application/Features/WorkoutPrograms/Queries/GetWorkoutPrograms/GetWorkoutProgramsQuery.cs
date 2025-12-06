using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutPrograms;

public class GetWorkoutProgramsQuery : IRequest<List<WorkoutProgramDto>>
{
    public Guid? CoachId { get; set; }
    public Guid? ClientId { get; set; }
}
