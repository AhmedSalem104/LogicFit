using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.RequestIdentityPasswordReset;

/// <summary>Always succeeds with an accepted response to prevent email-account enumeration.</summary>
public sealed record RequestIdentityPasswordResetCommand(string Email) : IRequest;

public sealed class RequestIdentityPasswordResetValidator : AbstractValidator<RequestIdentityPasswordResetCommand>
{
    public RequestIdentityPasswordResetValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
