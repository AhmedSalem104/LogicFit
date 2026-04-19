using LogicFit.Application.Features.Shifts.Commands.AssignShift;
using LogicFit.Application.Features.Shifts.Commands.CreateShift;
using LogicFit.Application.Features.Shifts.DTOs;
using LogicFit.Application.Features.Shifts.Queries.GetShiftAssignments;
using LogicFit.Application.Features.Shifts.Queries.GetShifts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Shifts;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShiftsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ShiftDto>>> Get([FromQuery] Guid? branchId, [FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetShiftsQuery { BranchId = branchId, IsActive = isActive }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateShiftCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("assign")]
    public async Task<ActionResult<Guid>> Assign(AssignShiftCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("assignments")]
    public async Task<ActionResult<List<ShiftAssignmentDto>>> GetAssignments(
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? shiftId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetShiftAssignmentsQuery
        {
            EmployeeId = employeeId,
            ShiftId = shiftId,
            FromDate = fromDate,
            ToDate = toDate
        }));
}
