using LogicFit.Application.Features.Platform.Subscriptions.Queries.GetPlatformSubscriptions;
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
}
