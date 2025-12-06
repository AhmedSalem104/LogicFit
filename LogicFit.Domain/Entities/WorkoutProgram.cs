using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class WorkoutProgram : TenantAuditableEntity
{
    public Guid CoachId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Navigation Properties
    public virtual User Coach { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
    public virtual ICollection<ProgramRoutine> Routines { get; set; } = new List<ProgramRoutine>();
}
