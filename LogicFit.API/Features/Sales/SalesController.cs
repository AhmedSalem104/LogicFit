using LogicFit.Application.Features.Sales.Commands.CheckoutSale;
using LogicFit.Application.Features.Sales.DTOs;
using LogicFit.Application.Features.Sales.Queries.GetSales;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Sales;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManagePOS)]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;
    public SalesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<SaleDto>>> GetSales(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? cashierId,
        [FromQuery] PaymentMethod? paymentMethod,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetSalesQuery
        {
            BranchId = branchId,
            ClientId = clientId,
            CashierId = cashierId,
            PaymentMethod = paymentMethod,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Checkout(CheckoutSaleCommand command)
        => Ok(await _mediator.Send(command));
}
