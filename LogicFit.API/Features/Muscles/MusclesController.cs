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

    [HttpGet]
    public async Task<ActionResult<List<MuscleDto>>> GetMuscles([FromQuery] string? bodyPart)
    {
        var result = await _mediator.Send(new GetMusclesQuery { BodyPart = bodyPart });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MuscleDto>> GetMuscle(int id)
    {
        var result = await _mediator.Send(new GetMuscleByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
