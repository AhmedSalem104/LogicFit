using MediatR;

namespace LogicFit.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommand : IRequest<bool>
{
    public Guid AttendanceId { get; set; }
}
