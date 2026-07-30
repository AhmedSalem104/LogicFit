using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.VerifyIdentityEmail;

public sealed record VerifyIdentityEmailCommand(string Token) : IRequest;

public sealed class VerifyIdentityEmailValidator : AbstractValidator<VerifyIdentityEmailCommand>
{
    public VerifyIdentityEmailValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}
