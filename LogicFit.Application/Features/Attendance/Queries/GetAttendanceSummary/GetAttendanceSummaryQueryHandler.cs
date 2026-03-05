using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Attendance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Queries.GetAttendanceSummary;

public class GetAttendanceSummaryQueryHandler : IRequestHandler<GetAttendanceSummaryQuery, AttendanceSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetAttendanceSummaryQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<AttendanceSummaryDto> Handle(GetAttendanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Attendances
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CheckInTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CheckInTime <= request.ToDate.Value);

        var attendances = await query.ToListAsync(cancellationToken);

        var completedSessions = attendances.Where(a => a.CheckOutTime.HasValue).ToList();

        var summary = new AttendanceSummaryDto
        {
            TotalCheckIns = attendances.Count,
            CheckedInNow = attendances.Count(a => a.CheckOutTime == null),
            AverageDurationMinutes = completedSessions.Count > 0
                ? completedSessions.Average(a => (a.CheckOutTime!.Value - a.CheckInTime).TotalMinutes)
                : 0,
            DailyBreakdown = attendances
                .GroupBy(a => a.CheckInTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new DailyAttendanceDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };

        return summary;
    }
}
