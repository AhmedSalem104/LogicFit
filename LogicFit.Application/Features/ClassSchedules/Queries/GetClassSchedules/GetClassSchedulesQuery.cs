using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClassSchedules.Queries.GetClassSchedules;

public class GetClassSchedulesQuery : IRequest<List<ClassScheduleDto>>
{
    public Guid? GroupClassId { get; set; }
    public Guid? CoachId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IncludeCancelled { get; set; }
}
