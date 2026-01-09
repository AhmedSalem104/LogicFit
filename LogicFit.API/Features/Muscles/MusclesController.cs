using LogicFit.Application.Features.Muscles.Commands.CreateMuscle;
using LogicFit.Application.Features.Muscles.Commands.DeleteMuscle;
using LogicFit.Application.Features.Muscles.Commands.UpdateMuscle;
using LogicFit.Application.Features.Muscles.DTOs;
using LogicFit.Application.Features.Muscles.Queries.GetMuscleById;
using LogicFit.Application.Features.Muscles.Queries.GetMuscles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Muscles;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MusclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MusclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all muscles with optional body part filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MuscleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MuscleDto>>> GetMuscles([FromQuery] string? bodyPart)
    {
        var result = await _mediator.Send(new GetMusclesQuery { BodyPart = bodyPart });
        return Ok(result);
    }

    /// <summary>
    /// Get a specific muscle by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MuscleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MuscleDto>> GetMuscle(int id)
    {
        var result = await _mediator.Send(new GetMuscleByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create a new muscle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MuscleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MuscleDto>> CreateMuscle([FromBody] CreateMuscleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMuscle), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing muscle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MuscleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MuscleDto>> UpdateMuscle(int id, [FromBody] UpdateMuscleCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a muscle (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMuscle(int id)
    {
        await _mediator.Send(new DeleteMuscleCommand { Id = id });
        return NoContent();
    }
}
