using LogicFit.Application.Features.Attendance.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Attendance.Queries.GetAttendances;

public class GetAttendancesQuery : IRequest<List<AttendanceDto>>
{
    public Guid? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? CheckedInOnly { get; set; }
}
