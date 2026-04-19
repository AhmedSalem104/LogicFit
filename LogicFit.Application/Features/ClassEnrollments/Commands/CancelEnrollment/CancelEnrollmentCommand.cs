using MediatR;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
}
