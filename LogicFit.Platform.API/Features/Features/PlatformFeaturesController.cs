using LogicFit.Application.Features.Platform.Features.Queries.GetFeatures;
using LogicFit.Application.Features.Platform.Features.Commands.UpsertFeature;
using LogicFit.Application.Features.Platform.Features.Commands.SetTenantOverride;
using LogicFit.Application.Features.Platform.Features.Queries.GetTenantOverrides;
using LogicFit.Application.Features.Platform.Features.Queries.GetQuotaDefinitions;
using LogicFit.Application.Features.Platform.Features.Commands.UpsertQuotaDefinition;
using LogicFit.Application.Features.Platform.Features.Commands.SetFeatureDependency;
using LogicFit.Application.Features.Platform.Features.Queries.GetFeatureDependencies;
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

    [HttpPost]
    public async Task<ActionResult<FeatureDto>> Create([FromBody] UpsertFeatureCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FeatureDto>> Update(Guid id, [FromBody] UpsertFeatureCommand command)
        => Ok(await _mediator.Send(new UpsertFeatureCommand
        {
            Id = id, Code = command.Code, NameAr = command.NameAr, NameEn = command.NameEn,
            Name = command.Name, Description = command.Description, Module = command.Module,
            IsFree = command.IsFree, IsActive = command.IsActive, SupportsQuota = command.SupportsQuota,
            Status = command.Status
        }));

    [HttpPost("tenant-overrides")]
    public async Task<ActionResult<Guid>> SetTenantOverride([FromBody] SetTenantOverrideCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("tenant-overrides")]
    public async Task<ActionResult<List<TenantFeatureOverrideDto>>> GetTenantOverrides([FromQuery] Guid? tenantId)
        => Ok(await _mediator.Send(new GetTenantOverridesQuery { TenantId = tenantId }));

    [HttpGet("quota-definitions")]
    public async Task<ActionResult<List<FeatureQuotaDefinitionDto>>> GetQuotaDefinitions()
        => Ok(await _mediator.Send(new GetQuotaDefinitionsQuery()));

    [HttpPost("quota-definitions")]
    public async Task<ActionResult<Guid>> CreateQuotaDefinition([FromBody] UpsertQuotaDefinitionCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("quota-definitions/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateQuotaDefinition(Guid id, [FromBody] UpsertQuotaDefinitionCommand command)
        => Ok(await _mediator.Send(new UpsertQuotaDefinitionCommand { Id = id, FeatureId = command.FeatureId, ResourceKey = command.ResourceKey, Unit = command.Unit, DefaultLimit = command.DefaultLimit, IsActive = command.IsActive }));

    [HttpGet("dependencies")]
    public async Task<ActionResult<List<FeatureDependencyDto>>> GetDependencies()
        => Ok(await _mediator.Send(new GetFeatureDependenciesQuery()));

    [HttpPost("dependencies")]
    public async Task<ActionResult<Guid>> SetDependency([FromBody] SetFeatureDependencyCommand command)
        => Ok(await _mediator.Send(command));
}
