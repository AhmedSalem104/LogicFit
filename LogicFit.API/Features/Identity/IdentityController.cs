using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity.Commands.IdentitySignIn;
using LogicFit.Application.Features.Identity.Commands.RegisterIdentity;
using LogicFit.Application.Features.Identity.Commands.RequestIdentityPasswordReset;
using LogicFit.Application.Features.Identity.Commands.ReissueApplicationTrackingSessions;
using LogicFit.Application.Features.Identity.Commands.ResetIdentityPassword;
using LogicFit.Application.Features.Identity.Commands.SelectIdentityWorkspace;
using LogicFit.Application.Features.Identity.Commands.VerifyIdentityEmail;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Identity;

/// <summary>
/// Identity-first sign-in. It proves a global identity, returns all active workspaces and pending
/// applications together, then exchanges the short-lived selection token for the existing tenant JWT.
/// </summary>
[ApiController]
[Route("api/identity")]
[AllowAnonymous]
public sealed class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator) => _mediator = mediator;

    [HttpPost("login")]
    [ProducesResponseType(typeof(IdentitySignInDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IdentitySignInDto>> Login([FromBody] IdentitySignInCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("register")]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Register([FromBody] RegisterIdentityCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyIdentityEmailCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset")]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestIdentityPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetIdentityPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("select-workspace")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> SelectWorkspace([FromBody] SelectIdentityWorkspaceCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("application-tracking-sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationTrackingSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicationTrackingSessionDto>>> ReissueApplicationTrackingSessions(
        [FromBody] ReissueApplicationTrackingSessionsCommand command,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));
}
