using LogicFit.Application.Features.Leaves.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Leaves.Queries.GetLeaveRequests;

public class GetLeaveRequestsQuery : IRequest<List<LeaveRequestDto>>
{
    public Guid? EmployeeId { get; set; }
    public LeaveStatus? Status { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
