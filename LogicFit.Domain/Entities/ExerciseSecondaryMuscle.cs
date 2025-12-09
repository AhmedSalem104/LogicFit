namespace LogicFit.Domain.Entities;

/// <summary>
/// Junction table linking exercises to their secondary (assistant) muscles with contribution percentages.
/// Example: Bench Press - Chest 60% (primary), Triceps 25%, Front Deltoid 15%
/// </summary>
public class ExerciseSecondaryMuscle
{
    public int ExerciseId { get; set; }
    public int MuscleId { get; set; }

    /// <summary>
    /// The percentage contribution of this muscle in the exercise (1-99).
    /// The primary muscle's contribution = 100 - sum of all secondary contributions.
    /// </summary>
    public int ContributionPercent { get; set; }

    // Navigation Properties
    public virtual Exercise Exercise { get; set; } = null!;
    public virtual Muscle Muscle { get; set; } = null!;
}
