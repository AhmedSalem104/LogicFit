namespace LogicFit.Application.Common.Interfaces;

/// <summary>Resolves a user's effective roles and permissions from the RBAC tables.</summary>
public interface IRbacService
{
    Task<UserAuthorization> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a system role (by name) to a user, if not already assigned.</summary>
    Task EnsureUserInRoleAsync(Guid userId, Guid? tenantId, string systemRoleName, CancellationToken cancellationToken = default);
}

public record UserAuthorization(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
