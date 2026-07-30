namespace LogicFit.Application.Common.Services;

public enum IdentityWorkspaceAccessMode
{
    Allowed,
    LegacyCompatibility,
    Blocked
}

/// <summary>Minimal, non-sensitive state required to enforce the identity and membership boundary.</summary>
public sealed record IdentityWorkspaceAccessState(
    bool UserExists,
    bool UserIsActive,
    bool IsIdentityLinked,
    bool IdentityIsActive,
    bool MembershipIsActive);

public sealed record IdentityWorkspaceAccessDecision(IdentityWorkspaceAccessMode Mode, string? Code = null);

/// <summary>
/// Pure access policy used by login, refresh, workspace selection, and request middleware.
/// Unlinked legacy accounts are an explicit compatibility mode, never equivalent to a verified
/// identity membership. The switch is retired only after the OTP migration.
/// </summary>
public static class IdentityWorkspaceAccessPolicy
{
    public static IdentityWorkspaceAccessDecision Evaluate(
        IdentityWorkspaceAccessState state,
        bool allowUnlinkedLegacySessions)
    {
        if (!state.UserExists)
            return Block("WORKSPACE_ACCOUNT_NOT_FOUND");

        if (state.IsIdentityLinked && !state.IdentityIsActive)
            return Block("IDENTITY_ACCOUNT_INACTIVE");

        if (state.IsIdentityLinked && !state.MembershipIsActive)
            return Block("WORKSPACE_MEMBERSHIP_INACTIVE");

        if (!state.UserIsActive)
            return Block("WORKSPACE_ACCOUNT_INACTIVE");

        if (!state.IsIdentityLinked)
        {
            return allowUnlinkedLegacySessions
                ? new IdentityWorkspaceAccessDecision(IdentityWorkspaceAccessMode.LegacyCompatibility, "LEGACY_IDENTITY_MIGRATION_REQUIRED")
                : Block("IDENTITY_MIGRATION_REQUIRED");
        }

        return new IdentityWorkspaceAccessDecision(IdentityWorkspaceAccessMode.Allowed);
    }

    private static IdentityWorkspaceAccessDecision Block(string code) =>
        new(IdentityWorkspaceAccessMode.Blocked, code);
}
