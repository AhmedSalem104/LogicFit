using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Equipment.Commands.ChangeEquipmentStatus;

public class ChangeEquipmentStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public EquipmentStatus Status { get; set; }
    public string? Notes { get; set; }
}
