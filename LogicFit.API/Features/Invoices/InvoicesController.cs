using LogicFit.Application.Features.Invoices.Commands.CancelInvoice;
using LogicFit.Application.Features.Invoices.Commands.CreateInvoice;
using LogicFit.Application.Features.Invoices.Commands.IssueInvoice;
using LogicFit.Application.Features.Invoices.DTOs;
using LogicFit.Application.Features.Invoices.Queries.GetInvoiceById;
using LogicFit.Application.Features.Invoices.Queries.GetInvoices;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Invoices;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageFinance)]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetInvoices(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? branchId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetInvoicesQuery
        {
            ClientId = clientId,
            BranchId = branchId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery { Id = id });
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateInvoiceCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("{id}/issue")]
    public async Task<ActionResult> Issue(Guid id)
    {
        await _mediator.Send(new IssueInvoiceCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelInvoiceCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
