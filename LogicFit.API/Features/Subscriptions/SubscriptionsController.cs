using LogicFit.Application.Features.Subscriptions.Commands.AddSubscriptionPayment;
using LogicFit.Application.Features.Subscriptions.Commands.CancelSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionFreeze;
using LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using LogicFit.Application.Features.Subscriptions.Commands.DeleteSubscriptionPlan;
using LogicFit.Application.Features.Subscriptions.Commands.EndFreezeEarly;
using LogicFit.Application.Features.Subscriptions.Commands.RenewSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.UpdateClientSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;
using LogicFit.Application.Features.Subscriptions.Queries.GetExpiringSubscriptions;
using LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionById;
using LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlanById;
using LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Subscriptions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ==================== Subscription Plans ====================

    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetSubscriptionPlans([FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetSubscriptionPlansQuery { IsActive = isActive });
        return Ok(result);
    }

    [HttpGet("plans/{id}")]
    public async Task<ActionResult<SubscriptionPlanDto>> GetSubscriptionPlan(Guid id)
    {
        var result = await _mediator.Send(new GetSubscriptionPlanByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("plans")]
    public async Task<ActionResult<Guid>> CreateSubscriptionPlan(CreateSubscriptionPlanCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("plans/{id}")]
    public async Task<ActionResult> UpdateSubscriptionPlan(Guid id, UpdateSubscriptionPlanCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("plans/{id}")]
    public async Task<ActionResult> DeleteSubscriptionPlan(Guid id)
    {
        await _mediator.Send(new DeleteSubscriptionPlanCommand { Id = id });
        return NoContent();
    }

    // ==================== Client Subscriptions ====================

    [HttpGet]
    public async Task<ActionResult<List<ClientSubscriptionDto>>> GetClientSubscriptions(
        [FromQuery] Guid? clientId,
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] Guid? planId,
        [FromQuery] int? expiringWithinDays,
        [FromQuery] string? searchTerm)
    {
        var result = await _mediator.Send(new GetClientSubscriptionsQuery
        {
            ClientId = clientId,
            Status = status,
            PlanId = planId,
            ExpiringWithinDays = expiringWithinDays,
            SearchTerm = searchTerm
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientSubscriptionDetailDto>> GetSubscription(Guid id)
    {
        var result = await _mediator.Send(new GetSubscriptionByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<List<ClientSubscriptionDto>>> GetExpiringSubscriptions([FromQuery] int days = 7)
    {
        var result = await _mediator.Send(new GetExpiringSubscriptionsQuery { Days = days });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateClientSubscription(CreateClientSubscriptionCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateClientSubscription(Guid id, UpdateClientSubscriptionCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{subscriptionId}/renew")]
    public async Task<ActionResult<Guid>> RenewSubscription(Guid subscriptionId, RenewSubscriptionCommand command)
    {
        command.SubscriptionId = subscriptionId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{subscriptionId}/payment")]
    public async Task<ActionResult> AddSubscriptionPayment(Guid subscriptionId, AddSubscriptionPaymentCommand command)
    {
        command.SubscriptionId = subscriptionId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{subscriptionId}/freeze")]
    public async Task<ActionResult<Guid>> CreateSubscriptionFreeze(Guid subscriptionId, CreateSubscriptionFreezeCommand command)
    {
        command.SubscriptionId = subscriptionId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{subscriptionId}/cancel")]
    public async Task<ActionResult> CancelSubscription(Guid subscriptionId, CancelSubscriptionCommand command)
    {
        command.SubscriptionId = subscriptionId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("freezes/{freezeId}/end")]
    public async Task<ActionResult> EndFreezeEarly(Guid freezeId)
    {
        await _mediator.Send(new EndFreezeEarlyCommand { FreezeId = freezeId });
        return NoContent();
    }
}
