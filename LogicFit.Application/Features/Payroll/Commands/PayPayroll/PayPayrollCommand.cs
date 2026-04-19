using MediatR;

namespace LogicFit.Application.Features.Payroll.Commands.PayPayroll;

public class PayPayrollCommand : IRequest
{
    public Guid Id { get; set; }
}
