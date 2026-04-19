using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetPosSalesReport;

public class GetPosSalesReportQuery : IRequest<PosSalesReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? BranchId { get; set; }
    public int TopProductsCount { get; set; } = 10;
}
