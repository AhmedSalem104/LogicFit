using MediatR;

namespace LogicFit.Application.Features.ClassSchedules.Commands.CancelClassSchedule;

public class CancelClassScheduleCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
}
