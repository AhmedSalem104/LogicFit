using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutSessions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessions;

public class GetWorkoutSessionsQueryHandler : IRequestHandler<GetWorkoutSessionsQuery, List<WorkoutSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetWorkoutSessionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<WorkoutSessionDto>> Handle(GetWorkoutSessionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.WorkoutSessions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Routine)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(s => s.ClientId == request.ClientId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(s => s.StartedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.StartedAt <= request.ToDate.Value);

        return await query
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new WorkoutSessionDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                ClientId = s.ClientId,
                ClientName = s.Client.Profile != null ? s.Client.Profile.FullName : s.Client.Email,
                RoutineId = s.RoutineId,
                RoutineName = s.Routine.Name,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                TotalVolumLifted = s.TotalVolumLifted,
                Notes = s.Notes
            })
            .ToListAsync(cancellationToken);
    }
}
