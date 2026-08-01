using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.RegisterIdentity;

/// <summary>
/// Starts global identity registration. The account remains unable to sign in until the recipient
/// redeems its one-time email verification link; registration grants no workspace access.
/// </summary>
public sealed record RegisterIdentityCommand(string FullName, string Email, string Password, string? PhoneNumber = null) : IRequest;

public sealed class RegisterIdentityValidator : AbstractValidator<RegisterIdentityCommand>
{
    public RegisterIdentityValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]")
            .Matches("[a-z]")
            .Matches("[0-9]");
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
    }
}
