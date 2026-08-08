using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;
using LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Platform.Tenants;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = Permissions.ManageTenants)]
public class PlatformTenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPlatformTenantLifecycleService _lifecycle;

    public PlatformTenantsController(IMediator mediator, IPlatformTenantLifecycleService lifecycle)
    {
        _mediator = mediator;
        _lifecycle = lifecycle;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] TenantStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPlatformTenantsQuery { Status = status, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PlatformTenantDto>> CreateTenant(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateTenantWithOwnerCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) &&
            (idempotencyKey.Length > 128 || idempotencyKey.Any(char.IsControl)))
        {
            return BadRequest(new
            {
                errorCode = "IDEMPOTENCY_KEY_INVALID",
                message = "Idempotency-Key must be at most 128 printable characters."
            });
        }

        command.IdempotencyKey = idempotencyKey?.Trim();
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTenants), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public Task<PlatformTenantDto> Approve(Guid id) => SetStatus(id, TenantStatus.Active);

    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public Task<PlatformTenantDto> Suspend(Guid id) => SetStatus(id, TenantStatus.Suspended);

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public Task<PlatformTenantDto> Activate(Guid id) => SetStatus(id, TenantStatus.Active);

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public Task<PlatformTenantDto> Archive(Guid id) => SetStatus(id, TenantStatus.Archived);

    [HttpGet("{id:guid}/credentials")]
    [ProducesResponseType(typeof(PlatformTenantCredentialsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformTenantCredentialsDto>> Credentials(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _lifecycle.GetCredentialsAsync(id, cancellationToken));

    [HttpPost("{id:guid}/credentials/reset")]
    [EnableRateLimiting("sensitive-action")]
    [ProducesResponseType(typeof(PlatformTenantPasswordResetDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<PlatformTenantPasswordResetDto>> ResetCredentials(
        Guid id,
        CancellationToken cancellationToken)
        => Accepted(await _lifecycle.RequestPasswordResetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/soft-delete")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformTenantDto>> SoftDelete(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _lifecycle.SoftDeleteAsync(id, cancellationToken));

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformTenantDto>> Restore(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _lifecycle.RestoreAsync(id, cancellationToken));

    [HttpPost("{id:guid}/permanent-delete")]
    [EnableRateLimiting("sensitive-action")]
    [ProducesResponseType(typeof(PlatformTenantPermanentDeleteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformTenantPermanentDeleteDto>> PermanentDelete(
        Guid id,
        [FromBody] PlatformTenantDeleteRequest request,
        CancellationToken cancellationToken)
    {
        EnsurePlatformOwner();
        return Ok(await _lifecycle.PermanentlyDeleteAsync(id, request, cancellationToken));
    }

    private Task<PlatformTenantDto> SetStatus(Guid id, TenantStatus status) =>
        _mediator.Send(new SetTenantStatusCommand { TenantId = id, Status = status });

    private void EnsurePlatformOwner()
    {
        if (!User.IsInRole(SystemRoles.PlatformOwner))
            throw new ForbiddenException("Only PlatformOwner can permanently delete a gym.");
    }
}
