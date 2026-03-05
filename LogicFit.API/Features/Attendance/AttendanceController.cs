using LogicFit.Application.Features.Attendance.Commands.CheckIn;
using LogicFit.Application.Features.Attendance.Commands.CheckOut;
using LogicFit.Application.Features.Attendance.Commands.DeleteAttendance;
using LogicFit.Application.Features.Attendance.DTOs;
using LogicFit.Application.Features.Attendance.Queries.GetAttendances;
using LogicFit.Application.Features.Attendance.Queries.GetAttendanceSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Attendance;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttendanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<AttendanceDto>>> GetAttendances(
        [FromQuery] Guid? clientId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? checkedInOnly)
    {
        var result = await _mediator.Send(new GetAttendancesQuery
        {
            ClientId = clientId,
            FromDate = fromDate,
            ToDate = toDate,
            CheckedInOnly = checkedInOnly
        });
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetAttendanceSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetAttendanceSummaryQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<Guid>> CheckIn(CheckInCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id}/check-out")]
    public async Task<ActionResult> CheckOut(Guid id)
    {
        await _mediator.Send(new CheckOutCommand { AttendanceId = id });
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAttendance(Guid id)
    {
        await _mediator.Send(new DeleteAttendanceCommand { Id = id });
        return NoContent();
    }
}
