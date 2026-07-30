namespace LogicFit.Application.Common.Services;

/// <summary>
/// Rollout switch for the identity-first migration. It defaults to compatibility so an existing
/// tenant-local account is never locked out before it can complete verified identity linking.
/// </summary>
public sealed class IdentityAccessOptions
{
    public const string SectionName = "Authentication:IdentityAccess";

    public bool AllowUnlinkedLegacySessions { get; init; } = true;
}
