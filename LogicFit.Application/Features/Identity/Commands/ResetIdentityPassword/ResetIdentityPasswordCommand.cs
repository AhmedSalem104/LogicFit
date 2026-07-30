using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.ResetIdentityPassword;

public sealed record ResetIdentityPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class ResetIdentityPasswordValidator : AbstractValidator<ResetIdentityPasswordCommand>
{
    public ResetIdentityPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]")
            .Matches("[a-z]")
            .Matches("[0-9]");
    }
}
