using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Leaves.DTOs;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int DurationDays => (ToDate - FromDate).Days + 1;
    public LeaveType LeaveType { get; set; }
    public string LeaveTypeName => LeaveType.ToString();
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public Guid? ReviewedById { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}
