using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.MarkAttended;

public class MarkAttendedCommandHandler : IRequestHandler<MarkAttendedCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public MarkAttendedCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(MarkAttendedCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var enrollment = await _context.ClassEnrollments
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ClassEnrollment", request.Id);

        if (enrollment.Status != ClassEnrollmentStatus.Booked)
            throw new DomainException("Only booked enrollments can be marked as attended");

        enrollment.Status = ClassEnrollmentStatus.Attended;
        enrollment.AttendedAt = _dateTimeService.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
