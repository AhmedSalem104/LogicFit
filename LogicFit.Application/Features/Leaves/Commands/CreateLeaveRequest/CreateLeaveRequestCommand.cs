using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Leaves.Commands.CreateLeaveRequest;

public class CreateLeaveRequestCommand : IRequest<Guid>
{
    public Guid EmployeeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public LeaveType LeaveType { get; set; }
    public string? Reason { get; set; }
}
