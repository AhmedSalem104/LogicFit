namespace LogicFit.Application.Features.Exercises.DTOs;

public class ExerciseDto
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
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
    public string? Icon { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? MovementPattern { get; set; }
    public string? Mechanic { get; set; }
    public string? Force { get; set; }

    // Instructions & Tips (stored as JSON arrays)
    public List<string>? Instructions { get; set; }
    public List<string>? InstructionsAr { get; set; }
    public List<string>? Tips { get; set; }
    public List<string>? TipsAr { get; set; }
    public List<string>? CommonMistakes { get; set; }
    public List<string>? CommonMistakesAr { get; set; }

    // Performance Guidelines
    public string? RepsRange { get; set; }
    public string? SetsRange { get; set; }
    public int? RestSeconds { get; set; }
    public string? Tempo { get; set; }
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
