using LogicFit.Application.Features.Subscriptions.Commands.CancelSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;
using LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionFreeze;
using LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;
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

    // Subscription Plans
    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetSubscriptionPlans()
    {
        var result = await _mediator.Send(new GetSubscriptionPlansQuery());
        return Ok(result);
    }

    [HttpPost("plans")]
    public async Task<ActionResult<Guid>> CreateSubscriptionPlan(CreateSubscriptionPlanCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    // Client Subscriptions
    [HttpGet]
    public async Task<ActionResult<List<ClientSubscriptionDto>>> GetClientSubscriptions(
        [FromQuery] Guid? clientId,
        [FromQuery] SubscriptionStatus? status)
    {
        var result = await _mediator.Send(new GetClientSubscriptionsQuery
        {
            ClientId = clientId,
            Status = status
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateClientSubscription(CreateClientSubscriptionCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{subscriptionId}/freeze")]
    public async Task<ActionResult<Guid>> CreateSubscriptionFreeze(Guid subscriptionId, CreateSubscriptionFreezeCommand command)
    {
        command.SubscriptionId = subscriptionId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{subscriptionId}/cancel")]
    public async Task<ActionResult> CancelSubscription(Guid subscriptionId)
    {
        await _mediator.Send(new CancelSubscriptionCommand { SubscriptionId = subscriptionId });
        return NoContent();
    }
}
