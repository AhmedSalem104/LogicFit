using LogicFit.Application.Features.GateAccess.Commands.GateCheckInByQr;
using LogicFit.Application.Features.GateAccess.DTOs;
using LogicFit.Application.Features.GateAccess.Queries.GetGateAccessLogs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.GateAccess;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageAttendance)]
public class GateAccessController : ControllerBase
{
    private readonly IMediator _mediator;

    public GateAccessController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("check-in-qr")]
    [ProducesResponseType(typeof(GateCheckInResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GateCheckInResultDto>> CheckInByQr(GateCheckInByQrCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(List<GateAccessLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GateAccessLogDto>>> GetLogs(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? branchId,
        [FromQuery] GateAccessResult? result,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int take = 200)
    {
        var logs = await _mediator.Send(new GetGateAccessLogsQuery
        {
            ClientId = clientId,
            BranchId = branchId,
            Result = result,
            FromDate = fromDate,
            ToDate = toDate,
            Take = take
        });
        return Ok(logs);
    }
}
