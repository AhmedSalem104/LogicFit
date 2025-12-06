using LogicFit.Application.Features.DietPlans.Commands.CreateDailyMeal;
using LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;
using LogicFit.Application.Features.DietPlans.Commands.CreateMealItem;
using LogicFit.Application.Features.DietPlans.Commands.DeleteDietPlan;
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

    [HttpPost("{planId}/meals")]
    public async Task<ActionResult<Guid>> CreateDailyMeal(Guid planId, CreateDailyMealCommand command)
    {
        command.PlanId = planId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("meals/{mealId}/items")]
    public async Task<ActionResult<Guid>> CreateMealItem(Guid mealId, CreateMealItemCommand command)
    {
        command.MealId = mealId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDietPlan(Guid id)
    {
        await _mediator.Send(new DeleteDietPlanCommand { Id = id });
        return NoContent();
    }
}
