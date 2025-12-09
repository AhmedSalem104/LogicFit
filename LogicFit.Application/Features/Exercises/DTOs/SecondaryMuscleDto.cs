namespace LogicFit.Application.Features.Exercises.DTOs;

/// <summary>
/// DTO for secondary muscle data in exercise response.
/// </summary>
public class SecondaryMuscleDto
{
    public int MuscleId { get; set; }
    public string MuscleName { get; set; } = string.Empty;
    public string? BodyPart { get; set; }
    public int ContributionPercent { get; set; }
}

/// <summary>
/// DTO for secondary muscle input when creating/updating exercises.
/// </summary>
public class SecondaryMuscleInputDto
{
    public int MuscleId { get; set; }
    public int ContributionPercent { get; set; }
}
