using LogicFit.Application.Features.Platform.Subscriptions.Queries.GetPlatformSubscriptions;
using LogicFit.Application.Features.Platform.Subscriptions.Queries.GetTenantUsage;
using LogicFit.Application.Features.Platform.Subscriptions.Commands.TransitionSubscription;
using LogicFit.Application.Features.Platform.Subscriptions.Commands.ExtendSubscription;
using LogicFit.Application.Features.Platform.Subscriptions.Queries.PreviewUpgrade;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Subscriptions;

[ApiController]
[Route("api/platform/subscriptions")]
[Authorize(Policy = Permissions.ManageTenants)]
public class PlatformSubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformSubscriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PlatformSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlatformSubscriptionDto>>> GetSubscriptions([FromQuery] TenantSubscriptionStatus? status)
    {
        var result = await _mediator.Send(new GetPlatformSubscriptionsQuery { Status = status });
        return Ok(result);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<List<TenantUsageDto>>> GetUsage()
        => Ok(await _mediator.Send(new GetTenantUsageQuery()));

    [HttpPost("{id:guid}/transition")]
    public async Task<ActionResult<TenantSubscriptionStatus>> Transition(Guid id, [FromBody] TransitionSubscriptionCommand command)
        => Ok(await _mediator.Send(new TransitionSubscriptionCommand { SubscriptionId = id, TargetStatus = command.TargetStatus }));

    [HttpPost("{id:guid}/extend")]
    public async Task<ActionResult<DateTime>> Extend(Guid id, [FromBody] ExtendSubscriptionCommand command)
        => Ok(await _mediator.Send(new ExtendSubscriptionCommand { SubscriptionId = id, Days = command.Days }));

    [HttpGet("{id:guid}/upgrade-preview/{targetPlanId:guid}")]
    public async Task<ActionResult<UpgradePreviewDto>> UpgradePreview(Guid id, Guid targetPlanId)
        => Ok(await _mediator.Send(new PreviewUpgradeQuery { SubscriptionId = id, TargetPlanId = targetPlanId }));
}
