using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetPayrollSummaryReport;

public class GetPayrollSummaryReportQuery : IRequest<PayrollSummaryReportDto>
{
    public int? Year { get; set; }
    public int? Month { get; set; }
}
