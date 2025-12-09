namespace LogicFit.Application.Features.Exercises.DTOs;

public class ExerciseDto
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public string? TargetMuscleName { get; set; }
    public string? TargetMuscleBodyPart { get; set; }

    /// <summary>
    /// Primary muscle contribution percentage (100 - sum of secondary muscles).
    /// Defaults to 100 if no secondary muscles defined.
    /// </summary>
    public int PrimaryMuscleContributionPercent { get; set; } = 100;

    /// <summary>
    /// Secondary muscles with their contribution percentages.
    /// Empty list if no secondary muscles defined (backward compatible).
    /// </summary>
    public List<SecondaryMuscleDto> SecondaryMuscles { get; set; } = new();

    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
}

public class CreateExerciseDto
{
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }

    /// <summary>
    /// Optional secondary muscles with contribution percentages.
    /// Sum of all contributions must not exceed 99%.
    /// </summary>
    public List<SecondaryMuscleInputDto>? SecondaryMuscles { get; set; }
}

public class UpdateExerciseDto
{
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }

    /// <summary>
    /// Optional secondary muscles with contribution percentages.
    /// Replaces all existing secondary muscles if provided.
    /// </summary>
    public List<SecondaryMuscleInputDto>? SecondaryMuscles { get; set; }
}
