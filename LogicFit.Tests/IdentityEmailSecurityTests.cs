using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.Commands.IdentitySignIn;
using LogicFit.Application.Features.Identity.Commands.RegisterIdentity;
using LogicFit.Application.Features.Identity.Commands.ResetIdentityPassword;
using Xunit;

namespace LogicFit.Tests;

public sealed class IdentityEmailSecurityTests
{
    [Fact]
    public void Email_action_token_is_opaque_and_only_its_sha256_hash_is_stable()
    {
        var token = IdentityEmailActionToken.CreateRaw();

        Assert.Equal(43, token.Length);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
        Assert.Equal(IdentityEmailActionToken.Hash(token), IdentityEmailActionToken.Hash(token));
        Assert.NotEqual(token, IdentityEmailActionToken.Hash(token));
    }

    [Fact]
    public void Email_normalization_is_case_and_whitespace_insensitive()
    {
        Assert.Equal("COACH@LOGICFIT.TEST", IdentityEmailAddress.Normalize("  Coach@LogicFit.test "));
    }

    [Fact]
    public void Global_identity_sign_in_rejects_phone_numbers()
    {
        var validation = new IdentitySignInValidator().Validate(new IdentitySignInCommand("01000000000", "Password1"));

        Assert.Contains(validation.Errors, x => x.PropertyName == nameof(IdentitySignInCommand.Email));
    }

    [Fact]
    public void Registration_requires_name_and_the_shared_strong_password_policy()
    {
        var validation = new RegisterIdentityValidator().Validate(
            new RegisterIdentityCommand("", "coach@logicfit.test", "weak"));

        Assert.Contains(validation.Errors, x => x.PropertyName == nameof(RegisterIdentityCommand.FullName));
        Assert.Contains(validation.Errors, x => x.PropertyName == nameof(RegisterIdentityCommand.Password));
    }

    [Fact]
    public void Email_password_reset_uses_the_shared_strong_password_policy()
    {
        var validation = new ResetIdentityPasswordValidator().Validate(
            new ResetIdentityPasswordCommand("opaque-token", "lowercase1"));

        Assert.Contains(validation.Errors, x => x.PropertyName == nameof(ResetIdentityPasswordCommand.NewPassword));
    }
}
