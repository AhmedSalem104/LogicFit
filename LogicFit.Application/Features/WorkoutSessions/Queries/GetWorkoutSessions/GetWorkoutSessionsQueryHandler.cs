using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutSessions.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessions;

public class GetWorkoutSessionsQueryHandler : IRequestHandler<GetWorkoutSessionsQuery, List<WorkoutSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkoutSessionsQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<WorkoutSessionDto>> Handle(GetWorkoutSessionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var currentUserRole = await _context.Users
            .Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);
        if (!currentUserRole.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        var query = _context.WorkoutSessions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Routine)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (currentUserRole == UserRole.Client)
        {
            query = query.Where(s => s.ClientId == currentUserId);
        }
        else if (currentUserRole is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            query = query.Where(s => _context.CoachClients.Any(cc => cc.TenantId == tenantId
                && cc.CoachId == currentUserId
                && cc.ClientId == s.ClientId
                && cc.IsActive
                && cc.UnassignedAt == null));
        }

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
