using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class GroupClass : TenantAuditableEntity
{
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Branch? Branch { get; set; }
    public virtual ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
}
