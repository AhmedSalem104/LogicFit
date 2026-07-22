using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GroupClasses.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassEnrollments.Commands.BookClass;

public class BookClassCommandHandler : IRequestHandler<BookClassCommand, ClassEnrollmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public BookClassCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    public async Task<ClassEnrollmentDto> Handle(BookClassCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var currentUserRole = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId)
            .Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);

        if (currentUserRole == UserRole.Client && request.ClientId != currentUserId)
            throw new ForbiddenException("Clients can only book classes for themselves");

        var schedule = await _context.ClassSchedules
            .Include(s => s.GroupClass)
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && s.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ClassSchedule", request.ScheduleId);

        if (schedule.IsCancelled)
            throw new DomainException("This class has been cancelled");
        if (schedule.StartTime < now)
            throw new DomainException("Cannot book a class that has already started");

        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && u.Role == UserRole.Client && u.IsActive && !u.IsDeleted, cancellationToken);
        if (!clientExists)
            throw new NotFoundException("Client", request.ClientId);

        var existing = schedule.Enrollments.FirstOrDefault(e => e.ClientId == request.ClientId
            && (e.Status == ClassEnrollmentStatus.Booked || e.Status == ClassEnrollmentStatus.Waitlist));
        if (existing != null)
            throw new ConflictException("Client is already enrolled in this class");

        var capacity = schedule.OverrideCapacity ?? schedule.GroupClass.Capacity;
        var bookedCount = schedule.Enrollments.Count(e => e.Status == ClassEnrollmentStatus.Booked);

        ClassEnrollmentStatus status;
        int? waitlistPosition = null;

        if (bookedCount < capacity)
        {
            status = ClassEnrollmentStatus.Booked;
        }
        else
        {
            status = ClassEnrollmentStatus.Waitlist;
            var currentWaitlist = schedule.Enrollments.Count(e => e.Status == ClassEnrollmentStatus.Waitlist);
            waitlistPosition = currentWaitlist + 1;
        }

        var enrollment = new ClassEnrollment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScheduleId = schedule.Id,
            ClientId = request.ClientId,
            EnrolledAt = now,
            Status = status,
            WaitlistPosition = waitlistPosition
        };

        _context.ClassEnrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClassEnrollmentDto
        {
            Id = enrollment.Id,
            ScheduleId = enrollment.ScheduleId,
            ClientId = enrollment.ClientId,
            EnrolledAt = enrollment.EnrolledAt,
            Status = enrollment.Status,
            WaitlistPosition = enrollment.WaitlistPosition
        };
    }
}
