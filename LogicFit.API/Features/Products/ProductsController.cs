using LogicFit.Application.Features.Products.Commands.CreateProduct;
using LogicFit.Application.Features.Products.Commands.DeleteProduct;
using LogicFit.Application.Features.Products.Commands.UpdateProduct;
using LogicFit.Application.Features.Products.DTOs;
using LogicFit.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Products;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageInventory)]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> Get(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] Guid? branchId)
        => Ok(await _mediator.Send(new GetProductsQuery
        {
            CategoryId = categoryId,
            IsActive = isActive,
            SearchTerm = searchTerm,
            LowStockOnly = lowStockOnly,
            BranchId = branchId
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateProductCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand { Id = id });
        return NoContent();
    }
}
