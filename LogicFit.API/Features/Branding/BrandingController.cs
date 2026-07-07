using LogicFit.Application.Features.Branding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Branding;

[ApiController]
[Route("api/[controller]")]
public class BrandingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrandingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Public white-label branding for a gym, by subdomain or custom domain (used to theme pre-login).</summary>
    [HttpGet("{identifier}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BrandingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandingDto>> GetBranding(string identifier)
    {
        var result = await _mediator.Send(new GetTenantBrandingQuery { Identifier = identifier });
        return result == null ? NotFound() : Ok(result);
    }
}
