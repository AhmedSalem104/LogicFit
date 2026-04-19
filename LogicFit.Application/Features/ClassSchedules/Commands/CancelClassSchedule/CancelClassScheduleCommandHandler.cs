using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassSchedules.Commands.CancelClassSchedule;

public class CancelClassScheduleCommandHandler : IRequestHandler<CancelClassScheduleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CancelClassScheduleCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(CancelClassScheduleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var schedule = await _context.ClassSchedules
            .Include(s => s.Enrollments)
            .Include(s => s.GroupClass)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ClassSchedule", request.Id);

        if (schedule.IsCancelled)
            throw new DomainException("Schedule is already cancelled");

        schedule.IsCancelled = true;
        schedule.CancellationReason = request.Reason;

        foreach (var enrollment in schedule.Enrollments.Where(e => e.Status == ClassEnrollmentStatus.Booked || e.Status == ClassEnrollmentStatus.Waitlist))
        {
            enrollment.Status = ClassEnrollmentStatus.Cancelled;
            enrollment.CancelledAt = now;
            enrollment.CancellationReason = "Class cancelled by staff";

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SenderId = enrollment.ClientId,
                RecipientId = enrollment.ClientId,
                Type = NotificationType.General,
                Title = "Class Cancelled",
                Body = $"The class '{schedule.GroupClass.Name}' scheduled for {schedule.StartTime:g} has been cancelled.",
                IsRead = false
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
