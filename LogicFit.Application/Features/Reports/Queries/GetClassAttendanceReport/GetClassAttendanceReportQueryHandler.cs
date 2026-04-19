using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetClassAttendanceReport;

public class GetClassAttendanceReportQueryHandler : IRequestHandler<GetClassAttendanceReportQuery, ClassAttendanceReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetClassAttendanceReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ClassAttendanceReportDto> Handle(GetClassAttendanceReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var from = request.FromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = request.ToDate ?? now;

        var schedulesQuery = _context.ClassSchedules
            .Include(s => s.GroupClass)
            .Include(s => s.Coach)
            .Include(s => s.Enrollments)
            .Where(s => s.TenantId == tenantId && s.StartTime >= from && s.StartTime <= to);

        if (request.BranchId.HasValue)
            schedulesQuery = schedulesQuery.Where(s => s.GroupClass.BranchId == request.BranchId.Value);

        var schedules = await schedulesQuery.ToListAsync(cancellationToken);

        var held = schedules.Where(s => !s.IsCancelled).ToList();
        var allEnrollments = held.SelectMany(s => s.Enrollments).ToList();

        var attended = allEnrollments.Count(e => e.Status == ClassEnrollmentStatus.Attended);
        var noShows = allEnrollments.Count(e => e.Status == ClassEnrollmentStatus.NoShow);
        var cancellations = allEnrollments.Count(e => e.Status == ClassEnrollmentStatus.Cancelled);
        var bookings = allEnrollments.Count(e => e.Status == ClassEnrollmentStatus.Booked || e.Status == ClassEnrollmentStatus.Attended);

        decimal attendanceRate = bookings > 0 ? Math.Round((decimal)attended / bookings * 100, 1) : 0;

        var avgFillRate = held
            .Where(s => s.GroupClass.Capacity > 0)
            .Select(s => (decimal)s.Enrollments.Count(e => e.Status == ClassEnrollmentStatus.Booked || e.Status == ClassEnrollmentStatus.Attended)
                / (s.OverrideCapacity ?? s.GroupClass.Capacity) * 100)
            .DefaultIfEmpty(0)
            .Average();

        var topClasses = held
            .GroupBy(s => new { s.GroupClassId, s.GroupClass.Name })
            .Select(g => new ClassPopularityDto
            {
                GroupClassId = g.Key.GroupClassId,
                ClassName = g.Key.Name,
                SchedulesCount = g.Count(),
                Bookings = g.SelectMany(s => s.Enrollments).Count(e => e.Status == ClassEnrollmentStatus.Booked || e.Status == ClassEnrollmentStatus.Attended),
                Attended = g.SelectMany(s => s.Enrollments).Count(e => e.Status == ClassEnrollmentStatus.Attended)
            })
            .Select(c =>
            {
                c.AttendanceRatePercent = c.Bookings > 0 ? Math.Round((decimal)c.Attended / c.Bookings * 100, 1) : 0;
                return c;
            })
            .OrderByDescending(c => c.Bookings)
            .Take(10)
            .ToList();

        var coachStats = held
            .Where(s => s.CoachId.HasValue)
            .GroupBy(s => new { s.CoachId, CoachName = s.Coach?.Email ?? "" })
            .Select(g => new CoachClassStatsDto
            {
                CoachId = g.Key.CoachId,
                CoachName = g.Key.CoachName,
                SchedulesCount = g.Count(),
                TotalAttendance = g.SelectMany(s => s.Enrollments).Count(e => e.Status == ClassEnrollmentStatus.Attended)
            })
            .OrderByDescending(c => c.TotalAttendance)
            .ToList();

        return new ClassAttendanceReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalSchedulesHeld = held.Count,
            CancelledSchedulesCount = schedules.Count(s => s.IsCancelled),
            TotalBookings = bookings,
            TotalAttended = attended,
            TotalNoShows = noShows,
            TotalCancellations = cancellations,
            AttendanceRatePercent = attendanceRate,
            AverageFillRatePercent = Math.Round(avgFillRate, 1),
            TopClasses = topClasses,
            CoachStats = coachStats
        };
    }
}
