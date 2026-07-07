using LogicFit.Application.Features.Equipment.Commands.ChangeEquipmentStatus;
using LogicFit.Application.Features.Equipment.Commands.CreateEquipment;
using LogicFit.Application.Features.Equipment.Commands.DeleteEquipment;
using LogicFit.Application.Features.Equipment.Commands.UpdateEquipment;
using LogicFit.Application.Features.Equipment.DTOs;
using LogicFit.Application.Features.Equipment.Queries.GetEquipment;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Equipment;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageBranches)]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EquipmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EquipmentDto>>> GetEquipment(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? roomId,
        [FromQuery] EquipmentStatus? status,
        [FromQuery] string? searchTerm)
    {
        var result = await _mediator.Send(new GetEquipmentQuery
        {
            BranchId = branchId,
            RoomId = roomId,
            Status = status,
            SearchTerm = searchTerm
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateEquipment(CreateEquipmentCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEquipment(Guid id, UpdateEquipmentCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> ChangeStatus(Guid id, ChangeEquipmentStatusCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEquipment(Guid id)
    {
        await _mediator.Send(new DeleteEquipmentCommand { Id = id });
        return NoContent();
    }
}
