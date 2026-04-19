using LogicFit.Application.Features.Shifts.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Shifts.Queries.GetShiftAssignments;

public class GetShiftAssignmentsQuery : IRequest<List<ShiftAssignmentDto>>
{
    public Guid? EmployeeId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
