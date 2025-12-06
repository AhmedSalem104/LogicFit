using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetCoachTraineesReport;

public class GetCoachTraineesReportQuery : IRequest<CoachTraineesReportDto>
{
    public Guid? CoachId { get; set; }  // If null, uses current user
}
