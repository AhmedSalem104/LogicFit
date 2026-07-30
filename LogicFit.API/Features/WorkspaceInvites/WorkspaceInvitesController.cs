using LogicFit.Application.Features.WorkspaceInvites.Commands.AcceptWorkspaceInvite;
using LogicFit.Application.Features.WorkspaceInvites.Commands.PreviewWorkspaceInvite;
using LogicFit.Application.Features.Identity.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace LogicFit.API.Features.WorkspaceInvites;

[ApiController]
[Route("api/workspace-invites")]
public sealed class WorkspaceInvitesController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkspaceInvitesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WorkspaceInvitePreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceInvitePreviewDto>> Preview(
        [FromBody] PreviewWorkspaceInviteCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptWorkspaceInviteCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
