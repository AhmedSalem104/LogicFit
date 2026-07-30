using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.Commands.LogoutAll;
using LogicFit.Application.Features.Auth.Commands.RefreshToken;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Platform.Auth.Commands.PlatformLogin;
using LogicFit.Application.Features.Platform.Auth.Commands.PlatformPasskeyLogin;
using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Platform.Auth;

[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] PlatformLoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Platform authentication begins with password verification and must complete a verified passkey assertion.</summary>
    [HttpPost("passkeys/login/options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyCeremonyOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyCeremonyOptionsDto>> BeginPasskeyLogin(
        [FromBody] BeginPlatformPasskeyLoginCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("passkeys/login/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> CompletePasskeyLogin(
        [FromBody] CompletePlatformPasskeyLoginCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    /// <summary>Initial enrollment only for an existing linked Platform Owner/Admin after password verification.</summary>
    [HttpPost("passkeys/registration/options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyCeremonyOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyCeremonyOptionsDto>> BeginPasskeyRegistration(
        [FromBody] BeginPlatformPasskeyRegistrationCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("passkeys/registration/verify")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompletePasskeyRegistration(
        [FromBody] CompletePlatformPasskeyRegistrationCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenCommand command)
    {
        command.Surface = RefreshTokenService.SurfacePlatform;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll()
    {
        await _mediator.Send(new LogoutAllCommand());
        return NoContent();
    }
}
