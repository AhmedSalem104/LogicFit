using LogicFit.API.Security;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity.Commands.IdentitySignIn;
using LogicFit.Application.Features.Identity.Commands.RegisterIdentity;
using LogicFit.Application.Features.Identity.Commands.RequestIdentityPasswordReset;
using LogicFit.Application.Features.Identity.Commands.ReissueApplicationTrackingSessions;
using LogicFit.Application.Features.Identity.Commands.ResetIdentityPassword;
using LogicFit.Application.Features.Identity.Commands.SelectIdentityWorkspace;
using LogicFit.Application.Features.Identity.Commands.VerifyIdentityEmail;
using LogicFit.Application.Features.Identity.Commands.Otp;
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
public sealed class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRefreshTokenCookieManager _refreshCookies;

    public IdentityController(IMediator mediator, IRefreshTokenCookieManager refreshCookies)
        => (_mediator, _refreshCookies) = (mediator, refreshCookies);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(IdentitySignInDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IdentitySignInDto>> Login([FromBody] IdentitySignInCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Register([FromBody] RegisterIdentityCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyIdentityEmailCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetIdentityPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return NoContent();
    }

    [HttpPost("select-workspace")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> SelectWorkspace([FromBody] SelectIdentityWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfaceTenant);
        return Ok(result);
    }

    [HttpPost("phone-login/request")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-request")]
    public async Task<ActionResult<OtpChallengeDto>> RequestPhoneLogin(
        [FromBody] RequestPhoneLoginOtpCommand command, CancellationToken cancellationToken)
        => Accepted(await _mediator.Send(command, cancellationToken));

    [HttpPost("phone-login/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult<IdentitySignInDto>> VerifyPhoneLogin(
        [FromBody] VerifyPhoneLoginOtpCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("phone/password-reset/request")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-request")]
    public async Task<ActionResult<OtpChallengeDto>> RequestPhonePasswordReset(
        [FromBody] RequestPhonePasswordResetOtpCommand command, CancellationToken cancellationToken)
        => Accepted(await _mediator.Send(command, cancellationToken));

    [HttpPost("phone/password-reset/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> ConfirmPhonePasswordReset(
        [FromBody] ResetPasswordWithPhoneOtpCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return NoContent();
    }

    [HttpPost("phone/request")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-request")]
    public async Task<ActionResult<OtpChallengeDto>> RequestPhoneVerification(
        [FromBody] RequestIdentityPhoneOtpCommand command, CancellationToken cancellationToken)
        => Accepted(await _mediator.Send(command, cancellationToken));

    [HttpPost("phone/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("otp-verify")]
    public async Task<IActionResult> VerifyPhone(
        [FromBody] VerifyIdentityPhoneOtpCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return NoContent();
    }

    [HttpPost("application-tracking-sessions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationTrackingSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicationTrackingSessionDto>>> ReissueApplicationTrackingSessions(
        [FromBody] ReissueApplicationTrackingSessionsCommand command,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));
}
