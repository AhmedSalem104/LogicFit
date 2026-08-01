using LogicFit.API.Security;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.Commands.ChangePassword;
using LogicFit.Application.Features.Auth.Commands.ForgetPassword;
using LogicFit.Application.Features.Auth.Commands.Login;
using LogicFit.Application.Features.Auth.Commands.LogoutAll;
using LogicFit.Application.Features.Auth.Commands.RefreshToken;
using LogicFit.Application.Features.Auth.Commands.Register;
using LogicFit.Application.Features.Auth.Commands.ResetPassword;
using LogicFit.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRefreshTokenCookieManager _refreshCookies;

    public AuthController(IMediator mediator, IRefreshTokenCookieManager refreshCookies)
    {
        _mediator = mediator;
        _refreshCookies = refreshCookies;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfaceTenant);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfaceTenant);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = _refreshCookies.Read(Request, RefreshTokenService.SurfaceTenant) ?? string.Empty,
            Surface = RefreshTokenService.SurfaceTenant
        };
        var result = await _mediator.Send(command);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfaceTenant);
        return Ok(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        await _mediator.Send(new LogoutAllCommand());
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return NoContent();
    }

    [HttpPost("forget-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(typeof(ForgetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgetPasswordResponse>> ForgetPassword([FromBody] ForgetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return Ok(result);
    }

    /// <summary>Authenticated self-service password change (current + new).</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        await _mediator.Send(command);
        _refreshCookies.Delete(Response, RefreshTokenService.SurfaceTenant);
        return NoContent();
    }
}
