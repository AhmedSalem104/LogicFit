using LogicFit.Application.Features.AthleteCheckins.Commands.CreateAthleteCheckin;
using LogicFit.Application.Features.AthleteCheckins.Commands.DeleteAthleteCheckin;
using LogicFit.Application.Features.AthleteCheckins.Commands.UpdateAthleteCheckin;
using LogicFit.Application.Features.AthleteCheckins.DTOs;
using LogicFit.Application.Features.AthleteCheckins.Queries.GetAthleteCheckins;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.AthleteCheckins;

/// <summary>Daily coaching readiness check-ins. These are not gym attendance records.</summary>
[ApiController]
[Route("api/clients/{clientId:guid}/checkins")]
[Authorize]
public sealed class AthleteCheckinsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AthleteCheckinsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(List<AthleteCheckinDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AthleteCheckinDto>>> Get(Guid clientId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetAthleteCheckinsQuery { ClientId = clientId, FromDate = fromDate, ToDate = toDate }));

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(Guid clientId, [FromBody] CreateAthleteCheckinCommand command)
    {
        command.ClientId = clientId;
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { clientId }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid clientId, Guid id, [FromBody] UpdateAthleteCheckinCommand command)
    {
        command.Id = id;
        command.ClientId = clientId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid clientId, Guid id)
    {
        await _mediator.Send(new DeleteAthleteCheckinCommand { Id = id, ClientId = clientId });
        return NoContent();
    }
}
