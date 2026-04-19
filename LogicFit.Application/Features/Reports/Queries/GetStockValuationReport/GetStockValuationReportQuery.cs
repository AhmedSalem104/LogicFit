using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetStockValuationReport;

public class GetStockValuationReportQuery : IRequest<StockValuationReportDto>
{
    public Guid? BranchId { get; set; }
}
