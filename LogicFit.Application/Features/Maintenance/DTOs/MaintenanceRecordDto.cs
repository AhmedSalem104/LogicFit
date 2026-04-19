using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Maintenance.DTOs;

public class MaintenanceRecordDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public decimal? Cost { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public string? TechnicianContact { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? ResolutionNotes { get; set; }
    public int? DurationDays => ResolvedDate.HasValue ? (ResolvedDate.Value - IssueDate).Days : null;
}
