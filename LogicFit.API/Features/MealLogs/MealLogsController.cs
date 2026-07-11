using LogicFit.Application.Features.MealLogs.Commands.DeleteMealLog;
using LogicFit.Application.Features.MealLogs.Commands.LogMeal;
using LogicFit.Application.Features.MealLogs.DTOs;
using LogicFit.Application.Features.MealLogs.Queries.GetMealLogs;
using LogicFit.Application.Features.MealLogs.Queries.GetNutritionSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.MealLogs;

/// <summary>Client food logging against their diet plan (the signed-in user is the client).</summary>
[ApiController]
[Route("api/meal-logs")]
[Authorize]
public class MealLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MealLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Log a consumed meal item (optionally an alternative food).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> LogMeal([FromBody] LogMealCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMealLogs), new { }, id);
    }

    /// <summary>The signed-in client's logged meals for a day (defaults to today), with computed macros.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MealLogDto>>> GetMealLogs([FromQuery] DateTime? date)
    {
        var result = await _mediator.Send(new GetMealLogsQuery { Date = date });
        return Ok(result);
    }

    /// <summary>Consumed macros for a day vs the client's active diet-plan targets.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(NutritionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NutritionSummaryDto>> GetSummary([FromQuery] DateTime? date)
    {
        var result = await _mediator.Send(new GetNutritionSummaryQuery { Date = date });
        return Ok(result);
    }

    /// <summary>Remove one of the client's own meal logs.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteMealLogCommand { Id = id });
        return NoContent();
    }
}
