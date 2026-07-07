using LogicFit.Application.Features.Suppliers.Commands.CreateSupplier;
using LogicFit.Application.Features.Suppliers.Commands.DeleteSupplier;
using LogicFit.Application.Features.Suppliers.Commands.UpdateSupplier;
using LogicFit.Application.Features.Suppliers.DTOs;
using LogicFit.Application.Features.Suppliers.Queries.GetSuppliers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Suppliers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageInventory)]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;
    public SuppliersController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> Get([FromQuery] bool? isActive, [FromQuery] string? searchTerm)
        => Ok(await _mediator.Send(new GetSuppliersQuery { IsActive = isActive, SearchTerm = searchTerm }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateSupplierCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateSupplierCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteSupplierCommand { Id = id });
        return NoContent();
    }
}
