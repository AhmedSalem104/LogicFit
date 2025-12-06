using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class CoachClient : TenantAuditableEntity
{
    public Guid CoachId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual User Coach { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
}
