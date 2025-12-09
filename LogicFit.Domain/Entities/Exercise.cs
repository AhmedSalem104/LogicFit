using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

public class Exercise : ISoftDeletable
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TargetMuscleId { get; set; }

    // Media
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }

    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }

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
