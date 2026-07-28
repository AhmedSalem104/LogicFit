using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.StaffAttendance.DTOs;

public sealed class StaffAttendanceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? EmployeeProfileId { get; set; }
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double? DurationMinutes { get; set; }
    public GateAccessMethod Method { get; set; }
    public bool IsOpen => !CheckOutTime.HasValue;
}
