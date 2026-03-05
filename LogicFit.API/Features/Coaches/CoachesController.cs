using LogicFit.Application.Features.Coaches.Commands.CreateCoach;
using LogicFit.Application.Features.Coaches.Commands.DeleteCoach;
using LogicFit.Application.Features.Coaches.Commands.UpdateCoach;
using LogicFit.Application.Features.Coaches.DTOs;
using LogicFit.Application.Features.Coaches.Queries.GetCoachById;
using LogicFit.Application.Features.Coaches.Queries.GetCoaches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Coaches;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoachesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoachesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CoachDto>>> GetCoaches(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetCoachesQuery
        {
            SearchTerm = searchTerm,
            IsActive = isActive
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CoachDto>> GetCoach(Guid id)
    {
        var result = await _mediator.Send(new GetCoachByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCoach(CreateCoachCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCoach(Guid id, UpdateCoachCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCoach(Guid id)
    {
        await _mediator.Send(new DeleteCoachCommand { Id = id });
        return NoContent();
    }
}
