using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetExpensesReport;

public class GetExpensesReportQuery : IRequest<ExpensesReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? BranchId { get; set; }
}
