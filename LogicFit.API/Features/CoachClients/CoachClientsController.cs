using LogicFit.Application.Features.CoachClients.Commands.AddTrainee;
using LogicFit.Application.Features.CoachClients.Commands.AssignClientToCoach;
using LogicFit.Application.Features.CoachClients.Commands.UnassignClientFromCoach;
using LogicFit.Application.Features.CoachClients.Commands.UpdateCoachClient;
using LogicFit.Application.Features.CoachClients.DTOs;
using LogicFit.Application.Features.CoachClients.Queries.GetCoachClientById;
using LogicFit.Application.Features.CoachClients.Queries.GetCoachClients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.CoachClients;

[ApiController]
[Route("api/coach-clients")]
[Authorize(Policy = Permissions.ManageCoaches)]
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
    /// Get a specific coach-client relationship by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CoachClientDto>> GetCoachClientById(Guid id)
    {
        var result = await _mediator.Send(new GetCoachClientByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Add a new trainee and assign to current coach
    /// Creates a new client account and automatically assigns to the logged-in coach
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> AddTrainee(AddTraineeCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    /// <summary>
    /// Assign an existing client to a coach
    /// - Owner: can assign to any coach
    /// - Coach: can only assign to self (leave coachId null)
    /// </summary>
    [HttpPost("assign")]
    public async Task<ActionResult<Guid>> AssignClientToCoach(AssignClientToCoachCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    /// <summary>
    /// Update a coach-client relationship
    /// - Transfer client to another coach (NewCoachId)
    /// - Activate/deactivate the relationship (IsActive)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCoachClient(Guid id, [FromBody] UpdateCoachClientCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result)
            return NotFound();
        return NoContent();
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
