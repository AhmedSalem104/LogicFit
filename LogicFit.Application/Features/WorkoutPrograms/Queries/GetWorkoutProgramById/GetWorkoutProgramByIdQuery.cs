using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutProgramById;

public class GetWorkoutProgramByIdQuery : IRequest<WorkoutProgramDto?>
{
    public Guid Id { get; set; }
}
