using LogicFit.Application.Features.Payroll.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Payroll.Queries.GetPayrollRuns;

public class GetPayrollRunsQuery : IRequest<List<PayrollRunDto>>
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public Guid? BranchId { get; set; }
    public PayrollStatus? Status { get; set; }
}
