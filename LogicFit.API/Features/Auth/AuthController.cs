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

namespace LogicFit.API.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenCommand command)
    {
        command.Surface = LogicFit.Application.Common.Services.RefreshTokenService.SurfaceTenant;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        await _mediator.Send(new LogoutAllCommand());
        return NoContent();
    }

    [HttpPost("forget-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ForgetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgetPasswordResponse>> ForgetPassword([FromBody] ForgetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
