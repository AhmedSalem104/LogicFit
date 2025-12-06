using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetSubscriptionsReport;

public class GetSubscriptionsReportQuery : IRequest<SubscriptionsReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
