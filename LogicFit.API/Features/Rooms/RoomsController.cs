using LogicFit.Application.Features.Rooms.Commands.CreateRoom;
using LogicFit.Application.Features.Rooms.Commands.DeleteRoom;
using LogicFit.Application.Features.Rooms.Commands.UpdateRoom;
using LogicFit.Application.Features.Rooms.DTOs;
using LogicFit.Application.Features.Rooms.Queries.GetRooms;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Rooms;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoomDto>>> GetRooms(
        [FromQuery] Guid? branchId,
        [FromQuery] RoomType? type,
        [FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetRoomsQuery { BranchId = branchId, Type = type, IsActive = isActive });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateRoom(CreateRoomCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateRoom(Guid id, UpdateRoomCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRoom(Guid id)
    {
        await _mediator.Send(new DeleteRoomCommand { Id = id });
        return NoContent();
    }
}
