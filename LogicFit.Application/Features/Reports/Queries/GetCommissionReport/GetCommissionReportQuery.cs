using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetCommissionReport;

public class GetCommissionReportQuery : IRequest<CommissionReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? EmployeeId { get; set; }
}
