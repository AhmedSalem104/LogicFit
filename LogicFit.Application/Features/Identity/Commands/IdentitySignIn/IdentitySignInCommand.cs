using FluentValidation;
using LogicFit.Application.Features.Identity.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.IdentitySignIn;

public sealed record IdentitySignInCommand(string Email, string Password) : IRequest<IdentitySignInDto>;

public sealed class IdentitySignInValidator : AbstractValidator<IdentitySignInCommand>
{
    public IdentitySignInValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
