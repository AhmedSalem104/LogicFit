using LogicFit.Application.Common.Services;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Evaluates whether a tenant-local user can use an authenticated workspace session.
/// The decision is separate from role permissions and subscription policy so every request path
/// applies the same identity/membership boundary before business authorization.
/// </summary>
public interface IIdentityWorkspaceAccessGuard
{
    Task<IdentityWorkspaceAccessDecision> EvaluateAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
