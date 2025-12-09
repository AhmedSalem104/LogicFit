using LogicFit.Application.Features.Exercises.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Application.Features.Exercises.Commands.UpdateExercise;

public class UpdateExerciseCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? Video { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }

    /// <summary>
    /// Secondary muscles with contribution percentages.
    /// If provided, replaces all existing secondary muscles.
    /// If null, existing secondary muscles are preserved.
    /// If empty list, all secondary muscles are removed.
    /// </summary>
    public List<SecondaryMuscleInputDto>? SecondaryMuscles { get; set; }
}
