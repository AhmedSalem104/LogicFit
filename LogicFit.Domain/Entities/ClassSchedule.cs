using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class ClassSchedule : TenantAuditableEntity
{
    public Guid GroupClassId { get; set; }
    public Guid? CoachId { get; set; }
    public Guid? RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
    public string? RecurrenceDaysOfWeek { get; set; } // e.g., "Mon,Wed,Fri"
    public DateTime? RecurrenceEndDate { get; set; }
    public int? OverrideCapacity { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    public virtual GroupClass GroupClass { get; set; } = null!;
    public virtual User? Coach { get; set; }
    public virtual Room? Room { get; set; }
    public virtual ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}
