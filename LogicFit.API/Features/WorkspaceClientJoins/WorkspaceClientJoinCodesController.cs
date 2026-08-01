using LogicFit.Application.Features.WorkspaceClientJoins.Commands.ApproveWorkspaceClientMembership;
using LogicFit.Application.Features.WorkspaceClientJoins.Commands.GenerateWorkspaceClientJoinCode;
using LogicFit.Application.Features.WorkspaceClientJoins.Commands.JoinWorkspaceAsClient;
using LogicFit.Application.Features.WorkspaceClientJoins.Commands.PreviewWorkspaceClientJoin;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.WorkspaceClientJoins;

[ApiController]
[Route("api/workspace/client-join-codes")]
[Authorize(Policy = Permissions.ManageMembers)]
public sealed class WorkspaceClientJoinCodesController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkspaceClientJoinCodesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Rotates the workspace's reusable client QR/join code. The raw code is returned once and never persisted.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceClientJoinCodeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkspaceClientJoinCodeDto>> Generate(
        [FromBody] GenerateWorkspaceClientJoinCodeCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _mediator.Send(command, cancellationToken));

    [HttpPost("memberships/{membershipId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid membershipId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ApproveWorkspaceClientMembershipCommand(membershipId), cancellationToken);
        return NoContent();
    }

    [HttpPost("preview")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-public-join")]
    [ProducesResponseType(typeof(WorkspaceClientJoinPreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceClientJoinPreviewDto>> Preview(
        [FromBody] PreviewWorkspaceClientJoinCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("join")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-public-join")]
    [ProducesResponseType(typeof(ClientJoinResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientJoinResultDto>> Join(
        [FromBody] JoinWorkspaceAsClientCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));
}
