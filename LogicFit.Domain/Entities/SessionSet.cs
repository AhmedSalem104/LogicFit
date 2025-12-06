using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class SessionSet : TenantAuditableEntity
{
    public Guid SessionId { get; set; }
    public int ExerciseId { get; set; }
    public int SetNumber { get; set; }

    // Actuals (Performed)
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public double? Rpe { get; set; }

    // Computed Field (calculated in Application layer before saving)
    public double VolumeLoad { get; set; }
    public bool IsPr { get; set; }

    // Navigation Properties
    public virtual WorkoutSession Session { get; set; } = null!;
    public virtual Exercise Exercise { get; set; } = null!;
}
