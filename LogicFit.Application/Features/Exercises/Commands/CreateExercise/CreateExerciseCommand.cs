using MediatR;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Application.Features.Exercises.Commands.CreateExercise;

public class CreateExerciseCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? Video { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
}
