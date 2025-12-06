namespace LogicFit.Application.Features.Exercises.DTOs;

public class ExerciseDto
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public string? TargetMuscleName { get; set; }
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
}

public class UpdateExerciseDto
{
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
}
