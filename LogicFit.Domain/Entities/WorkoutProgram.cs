using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class WorkoutProgram : TenantAuditableEntity
{
    public Guid CoachId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? Difficulty { get; set; }
    public int? DaysPerWeek { get; set; }
    public PlanStatus Status { get; set; } = PlanStatus.Active;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; } = 1;

    // Navigation Properties
    public virtual User Coach { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
    public virtual ICollection<ProgramRoutine> Routines { get; set; } = new List<ProgramRoutine>();
}
