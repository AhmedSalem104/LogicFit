using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Central authorization boundary for coach-created plans and client execution data.
/// Tenant filtering alone is not enough: a coach must also have an active assignment to
/// the client he is managing, while an owner/manager may manage clients in the same tenant.
/// </summary>
public interface ICoachPlanAccessService
{
    Task<UserRole> GetCurrentRoleAsync(CancellationToken cancellationToken = default);
    Task EnsureCanManageClientAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task EnsureCanManageWorkoutProgramAsync(Guid programId, CancellationToken cancellationToken = default);
    Task EnsureCanManageDietPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task EnsureCanManageRoutineAsync(Guid routineId, CancellationToken cancellationToken = default);
    Task EnsureCanManageMealAsync(Guid mealId, CancellationToken cancellationToken = default);
    Task EnsureClientOwnsRoutineAsync(Guid routineId, CancellationToken cancellationToken = default);
    Task EnsureClientOwnsSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
