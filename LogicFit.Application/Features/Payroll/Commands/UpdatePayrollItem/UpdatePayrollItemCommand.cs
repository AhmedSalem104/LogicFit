using MediatR;

namespace LogicFit.Application.Features.Payroll.Commands.UpdatePayrollItem;

public class UpdatePayrollItemCommand : IRequest
{
    public Guid Id { get; set; }
    public decimal? Bonus { get; set; }
    public decimal? Deductions { get; set; }
    public string? Notes { get; set; }
}
