using LogicFit.Application.Features.Tenants.Commands.CreateTenant;
using LogicFit.Application.Features.Tenants.DTOs;
using LogicFit.Application.Features.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Tenants;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantDto>>> GetTenants()
    {
        var result = await _mediator.Send(new GetTenantsQuery());
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTenants), new { id = result.Id }, result);
    }
}
