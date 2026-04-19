using MediatR;

namespace LogicFit.Application.Features.Payroll.Commands.GeneratePayroll;

public class GeneratePayrollCommand : IRequest<Guid>
{
    public int Month { get; set; }
    public int Year { get; set; }
    public Guid? BranchId { get; set; }
}
