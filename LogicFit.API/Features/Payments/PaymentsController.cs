using LogicFit.Application.Features.Payments.Commands.RecordPayment;
using LogicFit.Application.Features.Payments.DTOs;
using LogicFit.Application.Features.Payments.Queries.GetPayments;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Payments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? invoiceId,
        [FromQuery] Guid? subscriptionId,
        [FromQuery] PaymentMethod? method,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetPaymentsQuery
        {
            ClientId = clientId,
            BranchId = branchId,
            InvoiceId = invoiceId,
            SubscriptionId = subscriptionId,
            Method = method,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Record(RecordPaymentCommand command)
        => Ok(await _mediator.Send(command));
}
