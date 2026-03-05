using MediatR;

namespace LogicFit.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public string? Notes { get; set; }
}
