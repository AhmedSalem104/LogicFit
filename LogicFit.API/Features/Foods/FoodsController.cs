using LogicFit.Application.Features.Foods.Commands.CreateFood;
using LogicFit.Application.Features.Foods.Commands.DeleteFood;
using LogicFit.Application.Features.Foods.Commands.UpdateFood;
using LogicFit.Application.Features.Foods.DTOs;
using LogicFit.Application.Features.Foods.Queries.GetFoodById;
using LogicFit.Application.Features.Foods.Queries.GetFoods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Foods;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FoodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<FoodDto>>> GetFoods(
        [FromQuery] string? category,
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isVerified)
    {
        var result = await _mediator.Send(new GetFoodsQuery
        {
            Category = category,
            SearchTerm = searchTerm,
            IsVerified = isVerified
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FoodDto>> GetFood(int id)
    {
        var result = await _mediator.Send(new GetFoodByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateFood(CreateFoodCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetFood), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateFood(int id, UpdateFoodCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFood(int id)
    {
        await _mediator.Send(new DeleteFoodCommand { Id = id });
        return NoContent();
    }
}
