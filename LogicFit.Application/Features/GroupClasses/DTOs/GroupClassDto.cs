using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.GroupClasses.DTOs;

public class GroupClassDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; }
    public int UpcomingSchedulesCount { get; set; }
}

public class ClassScheduleDto
{
    public Guid Id { get; set; }
    public Guid GroupClassId { get; set; }
    public string? GroupClassName { get; set; }
    public string? Color { get; set; }
    public Guid? CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; }
    public string? RecurrenceDaysOfWeek { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public int? OverrideCapacity { get; set; }
    public int EffectiveCapacity { get; set; }
    public int BookedCount { get; set; }
    public int WaitlistCount { get; set; }
    public bool IsFull => BookedCount >= EffectiveCapacity;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
}

public class ClassEnrollmentDto
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime EnrolledAt { get; set; }
    public ClassEnrollmentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int? WaitlistPosition { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? AttendedAt { get; set; }
}
