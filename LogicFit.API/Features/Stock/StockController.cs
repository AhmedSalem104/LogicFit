using LogicFit.Application.Features.Stock.Commands.AdjustStock;
using LogicFit.Application.Features.Stock.Commands.TransferStock;
using LogicFit.Application.Features.Stock.DTOs;
using LogicFit.Application.Features.Stock.Queries.GetStock;
using LogicFit.Application.Features.Stock.Queries.GetStockMovements;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Stock;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageInventory)]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;
    public StockController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<StockItemDto>>> GetStock(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? productId,
        [FromQuery] bool? lowStockOnly)
        => Ok(await _mediator.Send(new GetStockQuery { BranchId = branchId, ProductId = productId, LowStockOnly = lowStockOnly }));

    [HttpGet("movements")]
    public async Task<ActionResult<List<StockMovementDto>>> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? branchId,
        [FromQuery] StockMovementType? type,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetStockMovementsQuery
        {
            ProductId = productId,
            BranchId = branchId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpPost("adjust")]
    public async Task<ActionResult> Adjust(AdjustStockCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("transfer")]
    public async Task<ActionResult> Transfer(TransferStockCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
