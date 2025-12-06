using LogicFit.Application.Features.WorkoutSessions.Commands.CreateSessionSet;
using LogicFit.Application.Features.WorkoutSessions.Commands.EndWorkoutSession;
using LogicFit.Application.Features.WorkoutSessions.Commands.StartWorkoutSession;
using LogicFit.Application.Features.WorkoutSessions.DTOs;
using LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessionById;
using LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkoutSessions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkoutSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkoutSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkoutSessionDto>>> GetWorkoutSessions(
        [FromQuery] Guid? clientId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetWorkoutSessionsQuery
        {
            ClientId = clientId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkoutSessionDto>> GetWorkoutSession(Guid id)
    {
        var result = await _mediator.Send(new GetWorkoutSessionByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("start")]
    public async Task<ActionResult<Guid>> StartWorkoutSession(StartWorkoutSessionCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWorkoutSession), new { id }, id);
    }

    [HttpPost("{sessionId}/end")]
    public async Task<ActionResult> EndWorkoutSession(Guid sessionId, EndWorkoutSessionCommand command)
    {
        command.SessionId = sessionId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{sessionId}/sets")]
    public async Task<ActionResult<Guid>> CreateSessionSet(Guid sessionId, CreateSessionSetCommand command)
    {
        command.SessionId = sessionId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }
}
