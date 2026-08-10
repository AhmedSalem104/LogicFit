using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutSessions.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessionById;

public class GetWorkoutSessionByIdQueryHandler : IRequestHandler<GetWorkoutSessionByIdQuery, WorkoutSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkoutSessionByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<WorkoutSessionDto?> Handle(GetWorkoutSessionByIdQuery request, CancellationToken cancellationToken)
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

        return await _context.WorkoutSessions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Routine)
            .Include(s => s.Sets).ThenInclude(st => st.Exercise)
            .Where(s => s.Id == request.Id && s.TenantId == tenantId
                && ((currentUserRole == UserRole.Client && s.ClientId == currentUserId)
                    || (currentUserRole == UserRole.Owner
                        || currentUserRole == UserRole.Manager
                        || currentUserRole == UserRole.FreelanceOwner)
                    || ((currentUserRole == UserRole.Coach
                        || currentUserRole == UserRole.Trainer
                        || currentUserRole == UserRole.FreelanceCoach)
                        && _context.CoachClients.Any(cc => cc.TenantId == tenantId
                            && cc.CoachId == currentUserId
                            && cc.ClientId == s.ClientId
                            && cc.IsActive
                            && cc.UnassignedAt == null))))
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
                Notes = s.Notes,
                Sets = s.Sets.Select(st => new SessionSetDto
                {
                    Id = st.Id,
                    SessionId = st.SessionId,
                    ExerciseId = st.ExerciseId,
                    ExerciseName = st.Exercise.Name,
                    SetNumber = st.SetNumber,
                    WeightKg = st.WeightKg,
                    Reps = st.Reps,
                    Rpe = st.Rpe,
                    VolumeLoad = st.VolumeLoad,
                    IsPr = st.IsPr
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
