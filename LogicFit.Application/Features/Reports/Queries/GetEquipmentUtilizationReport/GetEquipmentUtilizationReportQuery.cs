using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetEquipmentUtilizationReport;

public class GetEquipmentUtilizationReportQuery : IRequest<EquipmentUtilizationReportDto>
{
    public Guid? BranchId { get; set; }
}
