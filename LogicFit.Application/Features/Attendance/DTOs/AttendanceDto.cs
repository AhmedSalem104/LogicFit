namespace LogicFit.Application.Features.Attendance.DTOs;

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }
    public double? DurationMinutes { get; set; }
}

public class AttendanceSummaryDto
{
    public int TotalCheckIns { get; set; }
    public int CheckedInNow { get; set; }
    public double AverageDurationMinutes { get; set; }
    public List<DailyAttendanceDto> DailyBreakdown { get; set; } = new();
}

public class DailyAttendanceDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
