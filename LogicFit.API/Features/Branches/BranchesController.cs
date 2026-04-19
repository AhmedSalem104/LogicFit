using LogicFit.Application.Features.Branches.Commands.CreateBranch;
using LogicFit.Application.Features.Branches.Commands.DeleteBranch;
using LogicFit.Application.Features.Branches.Commands.SetOperatingHours;
using LogicFit.Application.Features.Branches.Commands.UpdateBranch;
using LogicFit.Application.Features.Branches.DTOs;
using LogicFit.Application.Features.Branches.Queries.GetBranchById;
using LogicFit.Application.Features.Branches.Queries.GetBranches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Branches;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BranchDto>>> GetBranches(
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
    {
        var result = await _mediator.Send(new GetBranchesQuery { IsActive = isActive, SearchTerm = searchTerm });
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchDto>> GetBranch(Guid id)
    {
        var result = await _mediator.Send(new GetBranchByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> CreateBranch(CreateBranchCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateBranch(Guid id, UpdateBranchCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteBranch(Guid id)
    {
        await _mediator.Send(new DeleteBranchCommand { Id = id });
        return NoContent();
    }

    [HttpPut("{id}/operating-hours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SetOperatingHours(Guid id, SetOperatingHoursCommand command)
    {
        command.BranchId = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
