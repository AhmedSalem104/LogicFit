using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

public class Exercise : ISoftDeletable
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public int TargetMuscleId { get; set; }

    // Media
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? Icon { get; set; }

    // Exercise Details
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? MovementPattern { get; set; }
    public string? Mechanic { get; set; }
    public string? Force { get; set; }

    // Instructions & Tips (stored as JSON)
    public string? Instructions { get; set; }
    public string? InstructionsAr { get; set; }
    public string? Tips { get; set; }
    public string? TipsAr { get; set; }
    public string? CommonMistakes { get; set; }
    public string? CommonMistakesAr { get; set; }

    // Performance Guidelines
    public string? RepsRange { get; set; }
    public string? SetsRange { get; set; }
    public int? RestSeconds { get; set; }
    public string? Tempo { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Muscle TargetMuscle { get; set; } = null!;
    public virtual ICollection<RoutineExercise> RoutineExercises { get; set; } = new List<RoutineExercise>();
    public virtual ICollection<SessionSet> SessionSets { get; set; } = new List<SessionSet>();
    public virtual ICollection<ExerciseSecondaryMuscle> SecondaryMuscles { get; set; } = new List<ExerciseSecondaryMuscle>();
}
