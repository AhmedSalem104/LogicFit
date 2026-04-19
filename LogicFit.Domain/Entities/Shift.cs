using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class Shift : TenantAuditableEntity
{
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Branch? Branch { get; set; }
}
