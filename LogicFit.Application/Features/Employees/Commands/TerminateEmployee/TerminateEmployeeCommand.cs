using MediatR;

namespace LogicFit.Application.Features.Employees.Commands.TerminateEmployee;

public class TerminateEmployeeCommand : IRequest
{
    public Guid Id { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? Reason { get; set; }
}
