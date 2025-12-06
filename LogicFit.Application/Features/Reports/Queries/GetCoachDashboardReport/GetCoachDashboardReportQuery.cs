using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetCoachDashboardReport;

public class GetCoachDashboardReportQuery : IRequest<CoachDashboardReportDto>
{
    public Guid? CoachId { get; set; }  // If null, uses current user
}
