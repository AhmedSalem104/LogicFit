using LogicFit.Application.Features.Platform.Features.Queries.GetFeatures;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Features;

[ApiController]
[Route("api/platform/features")]
[Authorize(Policy = Permissions.ManagePlans)]
public class PlatformFeaturesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformFeaturesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FeatureDto>>> GetFeatures()
    {
        var result = await _mediator.Send(new GetFeaturesQuery());
        return Ok(result);
    }
}
