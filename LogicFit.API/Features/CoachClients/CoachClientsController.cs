using LogicFit.Application.Features.CoachClients.Commands.AssignClientToCoach;
using LogicFit.Application.Features.CoachClients.Commands.UnassignClientFromCoach;
using LogicFit.Application.Features.CoachClients.DTOs;
using LogicFit.Application.Features.CoachClients.Queries.GetCoachClients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.CoachClients;

[ApiController]
[Route("api/coach-clients")]
[Authorize]
public class CoachClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoachClientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get trainees assigned to a coach
    /// - Owner: can view any coach's trainees (use coachId filter)
    /// - Coach: sees only their own trainees
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CoachClientDto>>> GetCoachClients(
        [FromQuery] Guid? coachId,
        [FromQuery] bool? isActive = true)
    {
        var result = await _mediator.Send(new GetCoachClientsQuery
        {
            CoachId = coachId,
            IsActive = isActive
        });
        return Ok(result);
    }

    /// <summary>
    /// Assign a client to a coach
    /// - Owner: can assign to any coach
    /// - Coach: can only assign to self (leave coachId null)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> AssignClientToCoach(AssignClientToCoachCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    /// <summary>
    /// Unassign a client from their coach
    /// - Owner: can unassign any client
    /// - Coach: can only unassign their own clients
    /// </summary>
    [HttpDelete("{clientId}")]
    public async Task<ActionResult> UnassignClientFromCoach(Guid clientId)
    {
        await _mediator.Send(new UnassignClientFromCoachCommand { ClientId = clientId });
        return NoContent();
    }
}
