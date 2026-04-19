using MediatR;

namespace LogicFit.Application.Features.Maintenance.Commands.CreateMaintenance;

public class CreateMaintenanceCommand : IRequest<Guid>
{
    public Guid EquipmentId { get; set; }
    public DateTime? IssueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public string? TechnicianContact { get; set; }
    public decimal? Cost { get; set; }
    public bool PutEquipmentUnderMaintenance { get; set; } = true;
}
