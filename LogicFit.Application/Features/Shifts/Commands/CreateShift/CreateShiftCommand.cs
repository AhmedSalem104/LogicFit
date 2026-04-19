using MediatR;

namespace LogicFit.Application.Features.Shifts.Commands.CreateShift;

public class CreateShiftCommand : IRequest<Guid>
{
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}
