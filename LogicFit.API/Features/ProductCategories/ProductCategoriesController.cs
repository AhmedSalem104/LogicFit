using LogicFit.Application.Features.ProductCategories.Commands.CreateProductCategory;
using LogicFit.Application.Features.ProductCategories.Commands.DeleteProductCategory;
using LogicFit.Application.Features.ProductCategories.Commands.UpdateProductCategory;
using LogicFit.Application.Features.ProductCategories.Queries.GetProductCategories;
using LogicFit.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.ProductCategories;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageInventory)]
public class ProductCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductCategoriesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ProductCategoryDto>>> Get([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetProductCategoriesQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateProductCategoryCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateProductCategoryCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCategoryCommand { Id = id });
        return NoContent();
    }
}
