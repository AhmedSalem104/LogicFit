using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Leaves.Commands.ReviewLeaveRequest;

public class ReviewLeaveRequestCommand : IRequest
{
    public Guid Id { get; set; }
    public LeaveStatus Decision { get; set; } // Approved or Rejected
    public string? Notes { get; set; }
}
