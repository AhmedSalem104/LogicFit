using MediatR;

namespace LogicFit.Application.Features.Payroll.Commands.ApprovePayroll;

public class ApprovePayrollCommand : IRequest
{
    public Guid Id { get; set; }
}
