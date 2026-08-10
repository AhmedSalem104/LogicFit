using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

public sealed class CoachPlanAccessService : ICoachPlanAccessService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CoachPlanAccessService(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<UserRole> GetCurrentRoleAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var tenantId = _tenantService.GetCurrentTenantId();
        var role = await _context.Users
            .Where(u => u.Id == userId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (!role.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        return role.Value;
    }

    public async Task EnsureCanManageClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();
        var role = await GetCurrentRoleAsync(cancellationToken);

        if (!CanManagePlans(role))
            throw new ForbiddenException("Only an owner, manager, coach, or trainer can manage plans.");

        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == clientId && u.TenantId == tenantId && u.Role == UserRole.Client && u.IsActive, cancellationToken);
        if (!clientExists)
            throw new NotFoundException("Client", clientId);

        if (CanManageAllClients(role))
            return;

        var assigned = await _context.CoachClients
            .AnyAsync(cc => cc.TenantId == tenantId
                && cc.ClientId == clientId
                && cc.CoachId == currentUserId
                && cc.IsActive
                && cc.UnassignedAt == null, cancellationToken);
        if (!assigned)
            throw new ForbiddenException("The client is not actively assigned to the current coach.");
    }

    public async Task EnsureCanManageWorkoutProgramAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = await _context.WorkoutPrograms
            .Where(p => p.Id == programId && p.TenantId == tenantId)
            .Select(p => (Guid?)p.ClientId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!clientId.HasValue)
            throw new NotFoundException("WorkoutProgram", programId);

        await EnsureCanManageClientAsync(clientId.Value, cancellationToken);
    }

    public async Task EnsureCanManageDietPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = await _context.DietPlans
            .Where(p => p.Id == planId && p.TenantId == tenantId)
            .Select(p => (Guid?)p.ClientId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!clientId.HasValue)
            throw new NotFoundException("DietPlan", planId);

        await EnsureCanManageClientAsync(clientId.Value, cancellationToken);
    }

    public async Task EnsureCanManageRoutineAsync(Guid routineId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = await _context.ProgramRoutines
            .Where(r => r.Id == routineId && r.TenantId == tenantId)
            .Select(r => (Guid?)r.Program.ClientId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!clientId.HasValue)
            throw new NotFoundException("ProgramRoutine", routineId);

        await EnsureCanManageClientAsync(clientId.Value, cancellationToken);
    }

    public async Task EnsureCanManageMealAsync(Guid mealId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = await _context.DailyMeals
            .Where(m => m.Id == mealId && m.TenantId == tenantId)
            .Select(m => (Guid?)m.Plan.ClientId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!clientId.HasValue)
            throw new NotFoundException("DailyMeal", mealId);

        await EnsureCanManageClientAsync(clientId.Value, cancellationToken);
    }

    public async Task EnsureClientOwnsRoutineAsync(Guid routineId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();
        var role = await GetCurrentRoleAsync(cancellationToken);
        if (role != UserRole.Client)
            throw new ForbiddenException("Only a client can start a workout session.");

        var belongsToClient = await _context.ProgramRoutines
            .AnyAsync(r => r.Id == routineId
                && r.TenantId == tenantId
                && r.Program.ClientId == currentUserId
                && r.Program.Status == PlanStatus.Active, cancellationToken);
        if (!belongsToClient)
            throw new ForbiddenException("This workout routine is not assigned to the current client.");
    }

    public async Task EnsureClientOwnsSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();
        var ownsSession = await _context.WorkoutSessions
            .AnyAsync(s => s.Id == sessionId && s.TenantId == tenantId && s.ClientId == currentUserId, cancellationToken);
        if (!ownsSession)
            throw new ForbiddenException("This workout session does not belong to the current client.");
    }

    private Guid GetCurrentUserId()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        return userId;
    }

    private static bool CanManagePlans(UserRole role) => role is
        UserRole.Owner or UserRole.Manager or UserRole.Coach or UserRole.Trainer or
        UserRole.FreelanceOwner or UserRole.FreelanceCoach;

    private static bool CanManageAllClients(UserRole role) => role is
        UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner;
}
