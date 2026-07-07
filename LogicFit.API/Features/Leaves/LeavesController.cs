using LogicFit.Application.Features.Leaves.Commands.CreateLeaveRequest;
using LogicFit.Application.Features.Leaves.Commands.ReviewLeaveRequest;
using LogicFit.Application.Features.Leaves.DTOs;
using LogicFit.Application.Features.Leaves.Queries.GetLeaveRequests;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Leaves;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageEmployees)]
public class LeavesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeavesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<LeaveRequestDto>>> Get(
        [FromQuery] Guid? employeeId,
        [FromQuery] LeaveStatus? status,
        [FromQuery] LeaveType? leaveType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetLeaveRequestsQuery
        {
            EmployeeId = employeeId,
            Status = status,
            LeaveType = leaveType,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateLeaveRequestCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("{id}/review")]
    public async Task<ActionResult> Review(Guid id, [FromBody] ReviewLeaveRequestCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
