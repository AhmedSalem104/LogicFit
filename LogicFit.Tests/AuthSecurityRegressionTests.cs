using LogicFit.Application.Features.Auth.Commands.ChangePassword;
using LogicFit.Application.Features.Auth.Commands.ResetPassword;
using Xunit;

namespace LogicFit.Tests;

public class AuthSecurityRegressionTests
{
    [Theory]
    [InlineData("short1A")]
    [InlineData("lowercase1")]
    [InlineData("UPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Reset_password_rejects_weak_passwords(string password)
    {
        var result = new ResetPasswordCommandValidator().Validate(new ResetPasswordCommand
        {
            PhoneNumber = "01000000000",
            ResetToken = "123456",
            NewPassword = password,
            Subdomain = "demo"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Change_password_accepts_the_shared_password_policy()
    {
        var result = new ChangePasswordCommandValidator().Validate(new ChangePasswordCommand
        {
            CurrentPassword = "OldPassword1",
            NewPassword = "NewPassword1"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }
}
