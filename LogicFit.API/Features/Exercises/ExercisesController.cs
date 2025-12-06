using LogicFit.Application.Features.Exercises.Commands.CreateExercise;
using LogicFit.Application.Features.Exercises.Commands.DeleteExercise;
using LogicFit.Application.Features.Exercises.Commands.UpdateExercise;
using LogicFit.Application.Features.Exercises.DTOs;
using LogicFit.Application.Features.Exercises.Queries.GetExerciseById;
using LogicFit.Application.Features.Exercises.Queries.GetExercises;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Exercises;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExercisesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExerciseDto>>> GetExercises(
        [FromQuery] int? targetMuscleId,
        [FromQuery] string? equipment,
        [FromQuery] bool? isHighImpact)
    {
        var result = await _mediator.Send(new GetExercisesQuery
        {
            TargetMuscleId = targetMuscleId,
            Equipment = equipment,
            IsHighImpact = isHighImpact
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(int id)
    {
        var result = await _mediator.Send(new GetExerciseByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<int>> CreateExercise([FromForm] CreateExerciseCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetExercise), new { id }, id);
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UpdateExercise(int id, [FromForm] UpdateExerciseCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExercise(int id)
    {
        await _mediator.Send(new DeleteExerciseCommand { Id = id });
        return NoContent();
    }
}
