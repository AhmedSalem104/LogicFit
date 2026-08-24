using LogicFit.Application.Features.WorkspaceMembers.Commands.ChangeWorkspaceMemberStatus;
using LogicFit.Application.Features.WorkspaceMembers.Commands.CreateWorkspaceMember;
using LogicFit.Application.Features.WorkspaceMembers.Commands.ResetWorkspaceMemberPassword;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Application.Features.WorkspaceMembers.Queries.GetWorkspaceMembers;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkspaceMembers;

/// <summary>
/// Owner-managed workspace access. Identity, tenant membership, role assignment and
/// one-time credentials are handled by the application commands behind this controller.
/// </summary>
[ApiController]
[Route("api/workspace-members")]
[Authorize(Policy = Permissions.ManageEmployees)]
[Authorize(Policy = WorkspaceCapabilities.GymStaff)]
public sealed class WorkspaceMembersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkspaceMembersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkspaceMemberDto>>> List(
        [FromQuery] UserRole? role,
        [FromQuery] string? accessStatus,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetWorkspaceMembersQuery
        {
            Role = role,
            AccessStatus = accessStatus,
            SearchTerm = searchTerm
        }, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceMemberCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkspaceMemberCreatedDto>> Create(
        [FromBody] CreateWorkspaceMemberCommand command,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _mediator.Send(command, cancellationToken));

    [HttpPost("{membershipId:guid}/suspend")]
    public Task<ActionResult<WorkspaceMemberDto>> Suspend(Guid membershipId, CancellationToken cancellationToken)
        => ChangeStatus(membershipId, WorkspaceMemberStatusAction.Suspend, cancellationToken);

    [HttpPost("{membershipId:guid}/activate")]
    public Task<ActionResult<WorkspaceMemberDto>> Activate(Guid membershipId, CancellationToken cancellationToken)
        => ChangeStatus(membershipId, WorkspaceMemberStatusAction.Activate, cancellationToken);

    [HttpPost("{membershipId:guid}/remove")]
    public Task<ActionResult<WorkspaceMemberDto>> Remove(Guid membershipId, CancellationToken cancellationToken)
        => ChangeStatus(membershipId, WorkspaceMemberStatusAction.Remove, cancellationToken);

    [HttpPost("{membershipId:guid}/reset-password")]
    [ProducesResponseType(typeof(WorkspaceMemberCreatedDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceMemberCreatedDto>> ResetPassword(Guid membershipId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ResetWorkspaceMemberPasswordCommand(membershipId), cancellationToken));

    private async Task<ActionResult<WorkspaceMemberDto>> ChangeStatus(
        Guid membershipId,
        WorkspaceMemberStatusAction action,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ChangeWorkspaceMemberStatusCommand(membershipId, action), cancellationToken));
}
