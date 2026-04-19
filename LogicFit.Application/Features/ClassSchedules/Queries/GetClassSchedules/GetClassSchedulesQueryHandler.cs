using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GroupClasses.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassSchedules.Queries.GetClassSchedules;

public class GetClassSchedulesQueryHandler : IRequestHandler<GetClassSchedulesQuery, List<ClassScheduleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClassSchedulesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClassScheduleDto>> Handle(GetClassSchedulesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ClassSchedules
            .Include(s => s.GroupClass)
            .Include(s => s.Coach)
            .Include(s => s.Room)
            .Include(s => s.Enrollments)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.GroupClassId.HasValue)
            query = query.Where(s => s.GroupClassId == request.GroupClassId.Value);
        if (request.CoachId.HasValue)
            query = query.Where(s => s.CoachId == request.CoachId.Value);
        if (request.RoomId.HasValue)
            query = query.Where(s => s.RoomId == request.RoomId.Value);
        if (request.BranchId.HasValue)
            query = query.Where(s => s.GroupClass.BranchId == request.BranchId.Value);
        if (request.FromDate.HasValue)
            query = query.Where(s => s.StartTime >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(s => s.StartTime <= request.ToDate.Value);
        if (request.IncludeCancelled != true)
            query = query.Where(s => !s.IsCancelled);

        var schedules = await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);

        return schedules.Select(s => new ClassScheduleDto
        {
            Id = s.Id,
            GroupClassId = s.GroupClassId,
            GroupClassName = s.GroupClass.Name,
            Color = s.GroupClass.Color,
            CoachId = s.CoachId,
            CoachName = s.Coach?.Email,
            RoomId = s.RoomId,
            RoomName = s.Room?.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            RecurrencePattern = s.RecurrencePattern,
            RecurrenceDaysOfWeek = s.RecurrenceDaysOfWeek,
            RecurrenceEndDate = s.RecurrenceEndDate,
            OverrideCapacity = s.OverrideCapacity,
            EffectiveCapacity = s.OverrideCapacity ?? s.GroupClass.Capacity,
            BookedCount = s.Enrollments.Count(e => e.Status == ClassEnrollmentStatus.Booked),
            WaitlistCount = s.Enrollments.Count(e => e.Status == ClassEnrollmentStatus.Waitlist),
            IsCancelled = s.IsCancelled,
            CancellationReason = s.CancellationReason
        }).ToList();
    }
}
