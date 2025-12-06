using LogicFit.Application.Features.Exercises.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Exercises.Queries.GetExercises;

public class GetExercisesQuery : IRequest<List<ExerciseDto>>
{
    public int? TargetMuscleId { get; set; }
    public string? Equipment { get; set; }
    public bool? IsHighImpact { get; set; }
}
