using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.RegisterIdentity;

/// <summary>Creates a global identity only; it grants no workspace access without approved membership.</summary>
public sealed record RegisterIdentityCommand(string Email, string? PhoneNumber, string Password) : IRequest;

public sealed class RegisterIdentityValidator : AbstractValidator<RegisterIdentityCommand>
{
    public RegisterIdentityValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
    }
}
