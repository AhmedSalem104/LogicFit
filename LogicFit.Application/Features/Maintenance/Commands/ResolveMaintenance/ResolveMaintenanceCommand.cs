using MediatR;

namespace LogicFit.Application.Features.Maintenance.Commands.ResolveMaintenance;

public class ResolveMaintenanceCommand : IRequest
{
    public Guid Id { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? FinalCost { get; set; }
    public bool ReactivateEquipment { get; set; } = true;
}
