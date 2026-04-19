using MediatR;

namespace LogicFit.Application.Features.Equipment.Commands.DeleteEquipment;

public class DeleteEquipmentCommand : IRequest
{
    public Guid Id { get; set; }
}
