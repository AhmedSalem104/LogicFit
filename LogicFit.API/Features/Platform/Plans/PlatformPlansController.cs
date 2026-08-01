using LogicFit.Application.Features.Platform.Plans.Commands.CreatePlan;
using LogicFit.Application.Features.Platform.Plans.Commands.DeletePlan;
using LogicFit.Application.Features.Platform.Plans.Commands.UpdatePlan;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Application.Features.Platform.Plans.Queries.GetPlans;
using LogicFit.Domain.Authorization;
using LogicFit.API.Features.Platform.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogicFit.Infrastructure.Authorization;

namespace LogicFit.API.Features.Platform.Plans;

[ApiController]
[Route("api/platform/plans")]
[Authorize(Policy = Permissions.ManagePlans)]
public class PlatformPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPlansQuery { ActiveOnly = activeOnly }, cancellationToken);
        return Ok(PlatformPaging.Create(result, page, pageSize));
    }

    [HttpPost]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlanDto>> CreatePlan([FromBody] CreatePlanCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPlans), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanDto>> UpdatePlan(Guid id, [FromBody] UpdatePlanCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _mediator.Send(new DeletePlanCommand(id));
        return NoContent();
    }
}
