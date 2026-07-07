using LogicFit.Application.Features.TenantBilling.Commands.ChooseSubscriptionPlan;
using LogicFit.Application.Features.TenantBilling.Commands.RenewSubscription;
using LogicFit.Application.Features.TenantBilling.DTOs;
using LogicFit.Application.Features.TenantBilling.Queries.GetMyInvoices;
using LogicFit.Application.Features.TenantBilling.Queries.GetMySubscription;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.TenantBilling;

[ApiController]
[Route("api/tenant")]
[Authorize(Policy = Permissions.ManageTenantBilling)]
public class TenantSubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantSubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Current plan, status, expiry, remaining days, plan limits and live usage.</summary>
    [HttpGet("my-subscription")]
    [ProducesResponseType(typeof(MySubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MySubscriptionDto>> GetMySubscription()
    {
        var result = await _mediator.Send(new GetMySubscriptionQuery());
        return Ok(result);
    }

    /// <summary>Usage vs plan limits (subset of my-subscription).</summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(MySubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetUsage()
    {
        var sub = await _mediator.Send(new GetMySubscriptionQuery());
        return Ok(new { sub.Members, sub.Coaches, sub.Branches, sub.Employees });
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(List<SubscriptionInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubscriptionInvoiceDto>>> GetInvoices()
    {
        var result = await _mediator.Send(new GetMyInvoicesQuery());
        return Ok(result);
    }

    [HttpPost("subscription/select-plan")]
    [ProducesResponseType(typeof(TenantSubscriptionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantSubscriptionSummaryDto>> SelectPlan([FromBody] ChooseSubscriptionPlanCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Upgrade to a different (typically higher) plan — same flow as selecting a plan.</summary>
    [HttpPost("subscription/upgrade")]
    [ProducesResponseType(typeof(TenantSubscriptionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantSubscriptionSummaryDto>> Upgrade([FromBody] ChooseSubscriptionPlanCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("subscription/renew")]
    [ProducesResponseType(typeof(TenantSubscriptionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantSubscriptionSummaryDto>> Renew()
    {
        var result = await _mediator.Send(new RenewSubscriptionCommand());
        return Ok(result);
    }
}
