using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class ClassEnrollment : TenantAuditableEntity
{
    public Guid ScheduleId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public ClassEnrollmentStatus Status { get; set; } = ClassEnrollmentStatus.Booked;
    public int? WaitlistPosition { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? AttendedAt { get; set; }

    public virtual ClassSchedule Schedule { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
}
