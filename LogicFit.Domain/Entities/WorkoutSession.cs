using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class WorkoutSession : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid RoutineId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double TotalVolumLifted { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
    public virtual ProgramRoutine Routine { get; set; } = null!;
    public virtual ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
}
