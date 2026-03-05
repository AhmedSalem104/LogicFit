using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class Attendance : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
}
