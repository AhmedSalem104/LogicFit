using LogicFit.Application.Features.Shifts.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Shifts.Queries.GetShifts;

public class GetShiftsQuery : IRequest<List<ShiftDto>>
{
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
}
