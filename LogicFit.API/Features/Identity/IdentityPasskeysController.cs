using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.Commands.Passkeys;
using LogicFit.Application.Features.Identity.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Identity;

[ApiController]
[Route("api/identity/passkeys")]
public sealed class IdentityPasskeysController : ControllerBase
{
    private readonly IMediator _mediator;
    public IdentityPasskeysController(IMediator mediator) => _mediator = mediator;

    [HttpPost("sign-in/options")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(typeof(PasskeyCeremonyOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyCeremonyOptionsDto>> BeginSignIn(
        [FromBody] BeginIdentityPasskeySignInCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("sign-in/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-email-actions")]
    [ProducesResponseType(typeof(IdentitySignInDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IdentitySignInDto>> CompleteSignIn(
        [FromBody] CompleteIdentityPasskeySignInCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("registration/options")]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyCeremonyOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyCeremonyOptionsDto>> BeginRegistration(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new BeginIdentityPasskeyRegistrationCommand(), cancellationToken));

    [HttpPost("registration/verify")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteRegistration(
        [FromBody] CompleteIdentityPasskeyRegistrationCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("step-up/options")]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyCeremonyOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyCeremonyOptionsDto>> BeginStepUp(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new BeginIdentityPasskeyStepUpCommand(), cancellationToken));

    [HttpPost("step-up/verify")]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyStepUpDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyStepUpDto>> CompleteStepUp(
        [FromBody] CompleteIdentityPasskeyStepUpCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));
}
