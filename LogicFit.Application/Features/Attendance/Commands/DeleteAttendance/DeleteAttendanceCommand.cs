using MediatR;

namespace LogicFit.Application.Features.Attendance.Commands.DeleteAttendance;

public class DeleteAttendanceCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
