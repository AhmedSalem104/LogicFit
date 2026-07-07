using LogicFit.Application.Features.MembershipCards.Commands.IssueMembershipCard;
using LogicFit.Application.Features.MembershipCards.Commands.RevokeMembershipCard;
using LogicFit.Application.Features.MembershipCards.DTOs;
using LogicFit.Application.Features.MembershipCards.Queries.GetMembershipCards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.MembershipCards;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageMembers)]
public class MembershipCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembershipCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MembershipCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MembershipCardDto>>> GetCards(
        [FromQuery] Guid? clientId,
        [FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetMembershipCardsQuery { ClientId = clientId, IsActive = isActive });
        return Ok(result);
    }

    [HttpPost("issue")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> IssueCard(IssueMembershipCardCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RevokeCard(Guid id, [FromBody] RevokeMembershipCardCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
