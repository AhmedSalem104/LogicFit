using LogicFit.Application.Features.Challenges.Commands.CreateChallenge;
using LogicFit.Application.Features.Challenges.Commands.DeleteChallenge;
using LogicFit.Application.Features.Challenges.Commands.JoinChallenge;
using LogicFit.Application.Features.Challenges.Commands.UpdateChallenge;
using LogicFit.Application.Features.Challenges.Commands.UpdateProgress;
using LogicFit.Application.Features.Challenges.DTOs;
using LogicFit.Application.Features.Challenges.Queries.GetChallengeById;
using LogicFit.Application.Features.Challenges.Queries.GetChallenges;
using LogicFit.Application.Features.Challenges.Queries.GetMyChallenges;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Challenges;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChallengesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChallengesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChallengeDto>>> GetChallenges([FromQuery] ChallengeStatus? status)
    {
        var result = await _mediator.Send(new GetChallengesQuery { Status = status });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChallengeDto>> GetChallengeById(Guid id)
    {
        var result = await _mediator.Send(new GetChallengeByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<ClientChallengeDto>>> GetMyChallenges()
    {
        var result = await _mediator.Send(new GetMyChallengesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateChallenge([FromBody] CreateChallengeCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetChallengeById), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateChallenge(Guid id, [FromBody] UpdateChallengeCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChallenge(Guid id)
    {
        await _mediator.Send(new DeleteChallengeCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<Guid>> JoinChallenge(Guid id)
    {
        var result = await _mediator.Send(new JoinChallengeCommand { ChallengeId = id });
        return Ok(result);
    }

    [HttpPut("{challengeId}/progress")]
    public async Task<ActionResult> UpdateProgress(Guid challengeId, [FromBody] UpdateProgressCommand command)
    {
        command.ChallengeId = challengeId;
        await _mediator.Send(command);
        return NoContent();
    }
}
