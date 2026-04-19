using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.BookClass;

public class BookClassCommand : IRequest<ClassEnrollmentDto>
{
    public Guid ScheduleId { get; set; }
    public Guid ClientId { get; set; }
}
