using LogicFit.Application.Features.DietPlans.Commands.CreateDailyMeal;
using LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;
using LogicFit.Application.Features.DietPlans.Commands.CreateMealItem;
using LogicFit.Application.Features.DietPlans.Commands.DeleteDailyMeal;
using LogicFit.Application.Features.DietPlans.Commands.DeleteDietPlan;
using LogicFit.Application.Features.DietPlans.Commands.DeleteMealItem;
using LogicFit.Application.Features.DietPlans.Commands.DuplicateDietPlan;
using LogicFit.Application.Features.DietPlans.Commands.UpdateDailyMeal;
using LogicFit.Application.Features.DietPlans.Commands.UpdateDietPlan;
using LogicFit.Application.Features.DietPlans.Commands.UpdateMealItem;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Application.Features.DietPlans.Queries.GetDietPlanById;
using LogicFit.Application.Features.DietPlans.Queries.GetDietPlans;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.DietPlans;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DietPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public DietPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<DietPlanDto>>> GetDietPlans(
        [FromQuery] Guid? coachId,
        [FromQuery] Guid? clientId,
        [FromQuery] PlanStatus? status)
    {
        var result = await _mediator.Send(new GetDietPlansQuery
        {
            CoachId = coachId,
            ClientId = clientId,
            Status = status
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DietPlanDto>> GetDietPlan(Guid id)
    {
        var result = await _mediator.Send(new GetDietPlanByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateDietPlan(CreateDietPlanCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDietPlan), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateDietPlan(Guid id, UpdateDietPlanCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDietPlan(Guid id)
    {
        await _mediator.Send(new DeleteDietPlanCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<Guid>> DuplicateDietPlan(Guid id, [FromBody] DuplicateDietPlanCommand? command)
    {
        command ??= new DuplicateDietPlanCommand();
        command.Id = id;
        var newId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDietPlan), new { id = newId }, newId);
    }

    // Meals endpoints
    [HttpPost("{planId}/meals")]
    public async Task<ActionResult<Guid>> CreateDailyMeal(Guid planId, CreateDailyMealCommand command)
    {
        command.PlanId = planId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("meals/{mealId}")]
    public async Task<ActionResult> UpdateDailyMeal(Guid mealId, UpdateDailyMealCommand command)
    {
        command.Id = mealId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("meals/{mealId}")]
    public async Task<ActionResult> DeleteDailyMeal(Guid mealId)
    {
        await _mediator.Send(new DeleteDailyMealCommand { Id = mealId });
        return NoContent();
    }

    // Meal Items endpoints
    [HttpPost("meals/{mealId}/items")]
    public async Task<ActionResult<Guid>> CreateMealItem(Guid mealId, CreateMealItemCommand command)
    {
        command.MealId = mealId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("meals/items/{itemId}")]
    public async Task<ActionResult> UpdateMealItem(Guid itemId, UpdateMealItemCommand command)
    {
        command.Id = itemId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("meals/items/{itemId}")]
    public async Task<ActionResult> DeleteMealItem(Guid itemId)
    {
        await _mediator.Send(new DeleteMealItemCommand { Id = itemId });
        return NoContent();
    }
}
