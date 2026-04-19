using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.ClassSchedules.Commands.CreateClassSchedule;

public class CreateClassScheduleCommand : IRequest<Guid>
{
    public Guid GroupClassId { get; set; }
    public Guid? CoachId { get; set; }
    public Guid? RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
    public string? RecurrenceDaysOfWeek { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public int? OverrideCapacity { get; set; }
}
