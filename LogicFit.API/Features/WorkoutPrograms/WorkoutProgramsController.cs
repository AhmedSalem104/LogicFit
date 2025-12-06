using LogicFit.Application.Features.WorkoutPrograms.Commands.CreateProgramRoutine;
using LogicFit.Application.Features.WorkoutPrograms.Commands.CreateRoutineExercise;
using LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;
using LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteWorkoutProgram;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutProgramById;
using LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutPrograms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkoutPrograms;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkoutProgramsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkoutProgramsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkoutProgramDto>>> GetWorkoutPrograms(
        [FromQuery] Guid? coachId,
        [FromQuery] Guid? clientId)
    {
        var result = await _mediator.Send(new GetWorkoutProgramsQuery
        {
            CoachId = coachId,
            ClientId = clientId
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkoutProgramDto>> GetWorkoutProgram(Guid id)
    {
        var result = await _mediator.Send(new GetWorkoutProgramByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateWorkoutProgram(CreateWorkoutProgramCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWorkoutProgram), new { id }, id);
    }

    [HttpPost("{programId}/routines")]
    public async Task<ActionResult<Guid>> CreateProgramRoutine(Guid programId, CreateProgramRoutineCommand command)
    {
        command.ProgramId = programId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("routines/{routineId}/exercises")]
    public async Task<ActionResult<Guid>> CreateRoutineExercise(Guid routineId, CreateRoutineExerciseCommand command)
    {
        command.RoutineId = routineId;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWorkoutProgram(Guid id)
    {
        await _mediator.Send(new DeleteWorkoutProgramCommand { Id = id });
        return NoContent();
    }
}
