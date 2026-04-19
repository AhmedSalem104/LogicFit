using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassSchedules.Commands.CreateClassSchedule;

public class CreateClassScheduleCommandHandler : IRequestHandler<CreateClassScheduleCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateClassScheduleCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateClassScheduleCommand request, CancellationToken cancellationToken)
    {
        if (request.EndTime <= request.StartTime)
            throw new DomainException("EndTime must be after StartTime");

        var tenantId = _tenantService.GetCurrentTenantId();

        var groupClass = await _context.GroupClasses
            .FirstOrDefaultAsync(g => g.Id == request.GroupClassId && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("GroupClass", request.GroupClassId);

        if (request.RoomId.HasValue)
        {
            var roomConflict = await _context.ClassSchedules
                .AnyAsync(s => s.RoomId == request.RoomId.Value
                    && s.TenantId == tenantId
                    && !s.IsCancelled
                    && s.StartTime < request.EndTime
                    && s.EndTime > request.StartTime, cancellationToken);
            if (roomConflict)
                throw new ConflictException("Room is already booked for this time");
        }

        if (request.CoachId.HasValue)
        {
            var coachConflict = await _context.ClassSchedules
                .AnyAsync(s => s.CoachId == request.CoachId.Value
                    && s.TenantId == tenantId
                    && !s.IsCancelled
                    && s.StartTime < request.EndTime
                    && s.EndTime > request.StartTime, cancellationToken);
            if (coachConflict)
                throw new ConflictException("Coach already has a scheduled class at this time");
        }

        var schedule = new ClassSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GroupClassId = request.GroupClassId,
            CoachId = request.CoachId,
            RoomId = request.RoomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RecurrencePattern = request.RecurrencePattern,
            RecurrenceDaysOfWeek = request.RecurrenceDaysOfWeek,
            RecurrenceEndDate = request.RecurrenceEndDate,
            OverrideCapacity = request.OverrideCapacity
        };

        _context.ClassSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }
}
