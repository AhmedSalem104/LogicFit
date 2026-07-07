using LogicFit.Application.Features.Platform.Dashboard;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Dashboard;

[ApiController]
[Route("api/platform/dashboard")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public class PlatformDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PlatformDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformDashboardDto>> Get()
    {
        var result = await _mediator.Send(new GetPlatformDashboardQuery());
        return Ok(result);
    }
}
