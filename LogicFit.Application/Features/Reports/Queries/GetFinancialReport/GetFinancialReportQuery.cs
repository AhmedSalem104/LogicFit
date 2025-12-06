using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetFinancialReport;

public class GetFinancialReportQuery : IRequest<FinancialReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
