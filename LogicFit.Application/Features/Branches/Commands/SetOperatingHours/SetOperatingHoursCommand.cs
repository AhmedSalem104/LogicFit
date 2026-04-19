using MediatR;

namespace LogicFit.Application.Features.Branches.Commands.SetOperatingHours;

public class SetOperatingHoursCommand : IRequest
{
    public Guid BranchId { get; set; }
    public List<OperatingHoursItem> Hours { get; set; } = new();
}

public class OperatingHoursItem
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
