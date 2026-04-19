using LogicFit.Application.Features.GroupClasses.Commands.CreateGroupClass;
using LogicFit.Application.Features.GroupClasses.Commands.DeleteGroupClass;
using LogicFit.Application.Features.GroupClasses.Commands.UpdateGroupClass;
using LogicFit.Application.Features.GroupClasses.DTOs;
using LogicFit.Application.Features.GroupClasses.Queries.GetGroupClasses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.GroupClasses;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupClassesController : ControllerBase
{
    private readonly IMediator _mediator;
    public GroupClassesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<GroupClassDto>>> GetClasses(
        [FromQuery] Guid? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] string? category)
        => Ok(await _mediator.Send(new GetGroupClassesQuery { BranchId = branchId, IsActive = isActive, Category = category }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateGroupClassCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateGroupClassCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteGroupClassCommand { Id = id });
        return NoContent();
    }
}
