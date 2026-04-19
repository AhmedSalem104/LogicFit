using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
