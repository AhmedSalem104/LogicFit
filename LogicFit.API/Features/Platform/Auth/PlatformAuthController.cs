using LogicFit.API.Security;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.Commands.LogoutAll;
using LogicFit.Application.Features.Auth.Commands.RefreshToken;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Platform.Auth.Commands.PlatformPasswordLogin;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Platform.Auth;

[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRefreshTokenCookieManager _refreshCookies;

    public PlatformAuthController(IMediator mediator, IRefreshTokenCookieManager refreshCookies)
    {
        _mediator = mediator;
        _refreshCookies = refreshCookies;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] PlatformPasswordLoginCommand command)
    {
        var result = await _mediator.Send(command);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfacePlatform);
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
            RefreshToken = _refreshCookies.Read(Request, RefreshTokenService.SurfacePlatform) ?? string.Empty,
            Surface = RefreshTokenService.SurfacePlatform
        };
        var result = await _mediator.Send(command);
        _refreshCookies.Write(Response, result.RefreshToken, RefreshTokenService.SurfacePlatform);
        return Ok(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll()
    {
        await _mediator.Send(new LogoutAllCommand());
        _refreshCookies.Delete(Response, RefreshTokenService.SurfacePlatform);
        return NoContent();
    }
}
