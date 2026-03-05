using LogicFit.Application.Features.Attendance.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Attendance.Queries.GetAttendanceSummary;

public class GetAttendanceSummaryQuery : IRequest<AttendanceSummaryDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
