using LogicFit.Application.Common.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class IdentityWorkspaceAccessPolicyTests
{
    [Fact]
    public void Active_linked_identity_membership_and_account_are_allowed()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, true, true, true, true),
            allowUnlinkedLegacySessions: true);

        Assert.Equal(IdentityWorkspaceAccessMode.Allowed, decision.Mode);
        Assert.Null(decision.Code);
    }

    [Fact]
    public void Inactive_identity_blocks_before_membership_and_user_state()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, false, true, false, false),
            allowUnlinkedLegacySessions: true);

        Assert.Equal(IdentityWorkspaceAccessMode.Blocked, decision.Mode);
        Assert.Equal("IDENTITY_ACCOUNT_INACTIVE", decision.Code);
    }

    [Fact]
    public void Inactive_membership_blocks_an_otherwise_active_account()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, true, true, true, false),
            allowUnlinkedLegacySessions: true);

        Assert.Equal(IdentityWorkspaceAccessMode.Blocked, decision.Mode);
        Assert.Equal("WORKSPACE_MEMBERSHIP_INACTIVE", decision.Code);
    }

    [Fact]
    public void Inactive_workspace_account_is_revoked_immediately()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, false, true, true, true),
            allowUnlinkedLegacySessions: true);

        Assert.Equal(IdentityWorkspaceAccessMode.Blocked, decision.Mode);
        Assert.Equal("WORKSPACE_ACCOUNT_INACTIVE", decision.Code);
    }

    [Fact]
    public void Unlinked_legacy_account_is_explicitly_compatibility_only_during_rollout()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, true, false, false, false),
            allowUnlinkedLegacySessions: true);

        Assert.Equal(IdentityWorkspaceAccessMode.LegacyCompatibility, decision.Mode);
        Assert.Equal("LEGACY_IDENTITY_MIGRATION_REQUIRED", decision.Code);
    }

    [Fact]
    public void Unlinked_legacy_account_is_blocked_when_the_migration_flag_is_disabled()
    {
        var decision = IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, true, false, false, false),
            allowUnlinkedLegacySessions: false);

        Assert.Equal(IdentityWorkspaceAccessMode.Blocked, decision.Mode);
        Assert.Equal("IDENTITY_MIGRATION_REQUIRED", decision.Code);
    }
}
