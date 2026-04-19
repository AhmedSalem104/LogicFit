using LogicFit.Application.Features.Rooms.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Rooms.Queries.GetRooms;

public class GetRoomsQuery : IRequest<List<RoomDto>>
{
    public Guid? BranchId { get; set; }
    public RoomType? Type { get; set; }
    public bool? IsActive { get; set; }
}
