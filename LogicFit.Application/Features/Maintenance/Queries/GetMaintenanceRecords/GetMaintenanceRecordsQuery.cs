using LogicFit.Application.Features.Maintenance.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Maintenance.Queries.GetMaintenanceRecords;

public class GetMaintenanceRecordsQuery : IRequest<List<MaintenanceRecordDto>>
{
    public Guid? EquipmentId { get; set; }
    public MaintenanceStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
