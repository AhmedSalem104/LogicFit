using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CancelEnrollmentCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(CancelEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var enrollment = await _context.ClassEnrollments
            .Include(e => e.Schedule)
                .ThenInclude(s => s.GroupClass)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ClassEnrollment", request.Id);

        if (enrollment.Status == ClassEnrollmentStatus.Cancelled)
            throw new DomainException("Enrollment is already cancelled");

        var wasBooked = enrollment.Status == ClassEnrollmentStatus.Booked;

        enrollment.Status = ClassEnrollmentStatus.Cancelled;
        enrollment.CancelledAt = now;
        enrollment.CancellationReason = request.Reason;
        enrollment.WaitlistPosition = null;

        if (wasBooked)
        {
            var firstOnWaitlist = await _context.ClassEnrollments
                .Where(e => e.ScheduleId == enrollment.ScheduleId
                    && e.TenantId == tenantId
                    && e.Status == ClassEnrollmentStatus.Waitlist)
                .OrderBy(e => e.WaitlistPosition)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstOnWaitlist != null)
            {
                firstOnWaitlist.Status = ClassEnrollmentStatus.Booked;
                firstOnWaitlist.WaitlistPosition = null;

                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SenderId = firstOnWaitlist.ClientId,
                    RecipientId = firstOnWaitlist.ClientId,
                    Type = NotificationType.General,
                    Title = "Class Booking Confirmed",
                    Body = $"You've been promoted from the waitlist to '{enrollment.Schedule.GroupClass.Name}' at {enrollment.Schedule.StartTime:g}.",
                    IsRead = false
                });

                var rest = await _context.ClassEnrollments
                    .Where(e => e.ScheduleId == enrollment.ScheduleId
                        && e.TenantId == tenantId
                        && e.Status == ClassEnrollmentStatus.Waitlist
                        && e.Id != firstOnWaitlist.Id)
                    .OrderBy(e => e.WaitlistPosition)
                    .ToListAsync(cancellationToken);

                int pos = 1;
                foreach (var e in rest) e.WaitlistPosition = pos++;
            }
        }
        else if (enrollment.Status == ClassEnrollmentStatus.Waitlist || enrollment.WaitlistPosition.HasValue)
        {
            var waitlist = await _context.ClassEnrollments
                .Where(e => e.ScheduleId == enrollment.ScheduleId
                    && e.TenantId == tenantId
                    && e.Status == ClassEnrollmentStatus.Waitlist
                    && e.Id != enrollment.Id)
                .OrderBy(e => e.WaitlistPosition)
                .ToListAsync(cancellationToken);

            int pos = 1;
            foreach (var e in waitlist) e.WaitlistPosition = pos++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
