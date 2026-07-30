using System.Text.Json;
using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.Passkeys;

public sealed record BeginIdentityPasskeyRegistrationCommand() : IRequest<PasskeyCeremonyOptionsDto>;
public sealed record CompleteIdentityPasskeyRegistrationCommand(Guid CeremonyId, JsonElement Credential, string? FriendlyName) : IRequest;
public sealed record BeginIdentityPasskeySignInCommand(string Email) : IRequest<PasskeyCeremonyOptionsDto>;
public sealed record CompleteIdentityPasskeySignInCommand(Guid CeremonyId, JsonElement Credential) : IRequest<IdentitySignInDto>;
public sealed record BeginIdentityPasskeyStepUpCommand() : IRequest<PasskeyCeremonyOptionsDto>;
public sealed record CompleteIdentityPasskeyStepUpCommand(Guid CeremonyId, JsonElement Credential) : IRequest<PasskeyStepUpDto>;

public sealed class BeginIdentityPasskeySignInValidator : AbstractValidator<BeginIdentityPasskeySignInCommand>
{
    public BeginIdentityPasskeySignInValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}

public sealed class CompleteIdentityPasskeyRegistrationValidator : AbstractValidator<CompleteIdentityPasskeyRegistrationCommand>
{
    public CompleteIdentityPasskeyRegistrationValidator()
    {
        RuleFor(x => x.CeremonyId).NotEmpty();
        RuleFor(x => x.Credential.ValueKind).NotEqual(JsonValueKind.Undefined);
        RuleFor(x => x.FriendlyName).MaximumLength(120);
    }
}

public sealed class CompleteIdentityPasskeySignInValidator : AbstractValidator<CompleteIdentityPasskeySignInCommand>
{
    public CompleteIdentityPasskeySignInValidator()
    {
        RuleFor(x => x.CeremonyId).NotEmpty();
        RuleFor(x => x.Credential.ValueKind).NotEqual(JsonValueKind.Undefined);
    }
}

public sealed class CompleteIdentityPasskeyStepUpValidator : AbstractValidator<CompleteIdentityPasskeyStepUpCommand>
{
    public CompleteIdentityPasskeyStepUpValidator()
    {
        RuleFor(x => x.CeremonyId).NotEmpty();
        RuleFor(x => x.Credential.ValueKind).NotEqual(JsonValueKind.Undefined);
    }
}

public sealed class BeginIdentityPasskeyRegistrationCommandHandler : IRequestHandler<BeginIdentityPasskeyRegistrationCommand, PasskeyCeremonyOptionsDto>
{
    private readonly IApplicationDbContext _context; private readonly ICurrentUserService _current; private readonly IIdentityPasskeyService _passkeys;
    public BeginIdentityPasskeyRegistrationCommandHandler(IApplicationDbContext context, ICurrentUserService current, IIdentityPasskeyService passkeys) => (_context, _current, _passkeys) = (context, current, passkeys);
    public async Task<PasskeyCeremonyOptionsDto> Handle(BeginIdentityPasskeyRegistrationCommand request, CancellationToken ct)
        => await _passkeys.BeginRegistrationAsync(await CurrentIdentityResolver.GetAsync(_context, _current, ct), ct);
}

public sealed class CompleteIdentityPasskeyRegistrationCommandHandler : IRequestHandler<CompleteIdentityPasskeyRegistrationCommand>
{
    private readonly IApplicationDbContext _context; private readonly ICurrentUserService _current; private readonly IIdentityPasskeyService _passkeys;
    public CompleteIdentityPasskeyRegistrationCommandHandler(IApplicationDbContext context, ICurrentUserService current, IIdentityPasskeyService passkeys) => (_context, _current, _passkeys) = (context, current, passkeys);
    public async Task Handle(CompleteIdentityPasskeyRegistrationCommand request, CancellationToken ct)
        => await _passkeys.CompleteRegistrationAsync(await CurrentIdentityResolver.GetAsync(_context, _current, ct), request.CeremonyId, request.Credential, request.FriendlyName, ct);
}

public sealed class BeginIdentityPasskeySignInCommandHandler : IRequestHandler<BeginIdentityPasskeySignInCommand, PasskeyCeremonyOptionsDto>
{
    private readonly IIdentityPasskeyService _passkeys; public BeginIdentityPasskeySignInCommandHandler(IIdentityPasskeyService passkeys) => _passkeys = passkeys;
    public Task<PasskeyCeremonyOptionsDto> Handle(BeginIdentityPasskeySignInCommand request, CancellationToken ct) => _passkeys.BeginSignInAsync(request.Email, ct);
}

public sealed class CompleteIdentityPasskeySignInCommandHandler : IRequestHandler<CompleteIdentityPasskeySignInCommand, IdentitySignInDto>
{
    private readonly IIdentityPasskeyService _passkeys; private readonly IIdentityWorkspaceSessionIssuer _issuer;
    public CompleteIdentityPasskeySignInCommandHandler(IIdentityPasskeyService passkeys, IIdentityWorkspaceSessionIssuer issuer) => (_passkeys, _issuer) = (passkeys, issuer);
    public async Task<IdentitySignInDto> Handle(CompleteIdentityPasskeySignInCommand request, CancellationToken ct)
        => await _issuer.IssueAsync(await _passkeys.CompleteSignInAsync(request.CeremonyId, request.Credential, ct), ct);
}

public sealed class BeginIdentityPasskeyStepUpCommandHandler : IRequestHandler<BeginIdentityPasskeyStepUpCommand, PasskeyCeremonyOptionsDto>
{
    private readonly IApplicationDbContext _context; private readonly ICurrentUserService _current; private readonly IIdentityPasskeyService _passkeys;
    public BeginIdentityPasskeyStepUpCommandHandler(IApplicationDbContext context, ICurrentUserService current, IIdentityPasskeyService passkeys) => (_context, _current, _passkeys) = (context, current, passkeys);
    public async Task<PasskeyCeremonyOptionsDto> Handle(BeginIdentityPasskeyStepUpCommand request, CancellationToken ct)
        => await _passkeys.BeginStepUpAsync(await CurrentIdentityResolver.GetAsync(_context, _current, ct), ct);
}

public sealed class CompleteIdentityPasskeyStepUpCommandHandler : IRequestHandler<CompleteIdentityPasskeyStepUpCommand, PasskeyStepUpDto>
{
    private readonly IApplicationDbContext _context; private readonly ICurrentUserService _current; private readonly IIdentityPasskeyService _passkeys;
    public CompleteIdentityPasskeyStepUpCommandHandler(IApplicationDbContext context, ICurrentUserService current, IIdentityPasskeyService passkeys) => (_context, _current, _passkeys) = (context, current, passkeys);
    public async Task<PasskeyStepUpDto> Handle(CompleteIdentityPasskeyStepUpCommand request, CancellationToken ct)
    {
        var identityId = await CurrentIdentityResolver.GetAsync(_context, _current, ct);
        return new PasskeyStepUpDto { Token = await _passkeys.CompleteStepUpAsync(identityId, request.CeremonyId, request.Credential, ct) };
    }
}

public sealed class PasskeyStepUpDto { public string Token { get; init; } = string.Empty; }

internal static class CurrentIdentityResolver
{
    public static async Task<Guid> GetAsync(IApplicationDbContext context, ICurrentUserService current, CancellationToken ct)
    {
        if (!Guid.TryParse(current.UserId, out var userId)) throw new UnauthorizedException("An authenticated workspace user is required.");
        var user = await context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct)
            ?? throw new UnauthorizedException("An authenticated workspace user is required.");
        if (!user.IdentityAccountId.HasValue) throw new ForbiddenException("Link a verified identity before registering a passkey.");
        return user.IdentityAccountId.Value;
    }
}
