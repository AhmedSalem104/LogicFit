using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceInvites.Commands.AcceptWorkspaceInvite;
using LogicFit.Application.Features.WorkspaceInvites.Commands.CreateWorkspaceInvite;
using LogicFit.Application.Features.WorkspaceInvites.Commands.PreviewWorkspaceInvite;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public sealed class WorkspaceInviteSecurityTests
{
    [Fact]
    public void Invite_token_is_opaque_and_hashable_without_persisting_the_raw_secret()
    {
        var token = IdentityEmailActionToken.CreateRaw();

        Assert.Equal(43, token.Length);
        Assert.NotEqual(token, IdentityEmailActionToken.Hash(token));
        Assert.Equal(IdentityEmailActionToken.Hash(token), IdentityEmailActionToken.Hash(token));
    }

    [Fact]
    public void Team_invitation_only_allows_freelance_team_roles()
    {
        var invalid = new CreateWorkspaceInviteValidator().Validate(
            new CreateWorkspaceInviteCommand("coach@logicfit.test", UserRole.Client));
        var valid = new CreateWorkspaceInviteValidator().Validate(
            new CreateWorkspaceInviteCommand("coach@logicfit.test", UserRole.FreelanceCoach));

        Assert.False(invalid.IsValid);
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void Invitation_preview_and_acceptance_require_both_secrets()
    {
        Assert.False(new PreviewWorkspaceInviteValidator().Validate(new PreviewWorkspaceInviteCommand("")).IsValid);
        Assert.False(new AcceptWorkspaceInviteValidator().Validate(new AcceptWorkspaceInviteCommand("invite", "")).IsValid);
    }
}
