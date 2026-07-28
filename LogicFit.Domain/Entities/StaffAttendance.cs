using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Attendance session for employees and coaches. Client attendance remains in Attendance.</summary>
public class StaffAttendance : TenantAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? EmployeeProfileId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public GateAccessMethod Method { get; set; } = GateAccessMethod.Manual;
    public string? Notes { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual EmployeeProfile? EmployeeProfile { get; set; }
    public virtual Branch? Branch { get; set; }
}
