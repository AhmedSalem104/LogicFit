namespace LogicFit.Application.Common.Interfaces;

/// <summary>Resolves a user's effective roles and permissions from the RBAC tables.</summary>
public interface IRbacService
{
    Task<UserAuthorization> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves only the role assignments for the selected workspace. This is
    /// required for identities that belong to more than one tenant.
    /// </summary>
    Task<UserAuthorization> GetUserAuthorizationForTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => GetUserAuthorizationAsync(userId, cancellationToken);

    /// <summary>Assigns a system role (by name) to a user, if not already assigned.</summary>
    Task EnsureUserInRoleAsync(Guid userId, Guid? tenantId, string systemRoleName, CancellationToken cancellationToken = default);
}

public record UserAuthorization(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
