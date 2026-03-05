using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Attendance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Queries.GetAttendances;

public class GetAttendancesQueryHandler : IRequestHandler<GetAttendancesQuery, List<AttendanceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetAttendancesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<AttendanceDto>> Handle(GetAttendancesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Attendances
            .Include(a => a.Client)
                .ThenInclude(c => c.Profile)
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(a => a.ClientId == request.ClientId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CheckInTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CheckInTime <= request.ToDate.Value);

        if (request.CheckedInOnly == true)
            query = query.Where(a => a.CheckOutTime == null);

        return await query
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client.Profile != null ? a.Client.Profile.FullName : null,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Notes = a.Notes,
                DurationMinutes = a.CheckOutTime.HasValue
                    ? (a.CheckOutTime.Value - a.CheckInTime).TotalMinutes
                    : null
            })
            .ToListAsync(cancellationToken);
    }
}
