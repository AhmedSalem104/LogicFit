using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class BranchOperatingHours : TenantAuditableEntity
{
    public Guid BranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
