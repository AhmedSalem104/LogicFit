using MediatR;

namespace LogicFit.Application.Features.Shifts.Commands.AssignShift;

public class AssignShiftCommand : IRequest<Guid>
{
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
