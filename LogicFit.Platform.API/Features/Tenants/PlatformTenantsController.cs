using LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;
using LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Tenants;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = Permissions.ManageTenants)]
public class PlatformTenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformTenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PlatformTenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlatformTenantDto>>> GetTenants([FromQuery] TenantStatus? status)
    {
        var result = await _mediator.Send(new GetPlatformTenantsQuery { Status = status });
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlatformTenantDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlatformTenantDto>> CreateTenant([FromBody] CreateTenantWithOwnerCommand command)
    {
        var result = await _mediator.Send(command);
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

    private Task<PlatformTenantDto> SetStatus(Guid id, TenantStatus status) =>
        _mediator.Send(new SetTenantStatusCommand { TenantId = id, Status = status });
}
