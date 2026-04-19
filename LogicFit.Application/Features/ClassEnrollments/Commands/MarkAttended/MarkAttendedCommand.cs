using MediatR;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.MarkAttended;

public class MarkAttendedCommand : IRequest
{
    public Guid Id { get; set; }
}
