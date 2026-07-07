using LogicFit.Application.Features.Platform.Plans.Commands.CreatePlan;
using LogicFit.Application.Features.Platform.Plans.Commands.DeletePlan;
using LogicFit.Application.Features.Platform.Plans.Commands.UpdatePlan;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Application.Features.Platform.Plans.Queries.GetPlans;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Plans;

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
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlanDto>>> GetPlans([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetPlansQuery { ActiveOnly = activeOnly });
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlanDto>> CreatePlan([FromBody] CreatePlanCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPlans), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanDto>> UpdatePlan(Guid id, [FromBody] UpdatePlanCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _mediator.Send(new DeletePlanCommand(id));
        return NoContent();
    }
}
