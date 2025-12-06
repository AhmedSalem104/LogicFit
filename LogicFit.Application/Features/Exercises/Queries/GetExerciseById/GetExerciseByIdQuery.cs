using LogicFit.Application.Features.Exercises.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Exercises.Queries.GetExerciseById;

public class GetExerciseByIdQuery : IRequest<ExerciseDto?>
{
    public int Id { get; set; }
}
