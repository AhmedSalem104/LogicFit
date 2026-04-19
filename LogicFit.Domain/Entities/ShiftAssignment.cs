using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ShiftAssignment : TenantAuditableEntity
{
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public string? Notes { get; set; }

    public virtual Shift Shift { get; set; } = null!;
    public virtual EmployeeProfile Employee { get; set; } = null!;
}
