using LogicFit.Application.Features.Equipment.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Equipment.Queries.GetEquipment;

public class GetEquipmentQuery : IRequest<List<EquipmentDto>>
{
    public Guid? BranchId { get; set; }
    public Guid? RoomId { get; set; }
    public EquipmentStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
}
