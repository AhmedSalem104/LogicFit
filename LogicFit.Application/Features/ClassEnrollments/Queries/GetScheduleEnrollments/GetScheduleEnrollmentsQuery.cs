using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClassEnrollments.Queries.GetScheduleEnrollments;

public class GetScheduleEnrollmentsQuery : IRequest<List<ClassEnrollmentDto>>
{
    public Guid ScheduleId { get; set; }
    public bool IncludeCancelled { get; set; }
}
