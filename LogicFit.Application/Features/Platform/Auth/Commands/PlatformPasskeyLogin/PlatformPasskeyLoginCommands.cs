using System.Text.Json;
using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Auth.Commands.PlatformPasskeyLogin;

public sealed record BeginPlatformPasskeyLoginCommand(string Email, string Password) : IRequest<PasskeyCeremonyOptionsDto>;
public sealed record CompletePlatformPasskeyLoginCommand(Guid CeremonyId, JsonElement Credential) : IRequest<AuthResponseDto>;
public sealed record BeginPlatformPasskeyRegistrationCommand(string Email, string Password) : IRequest<PasskeyCeremonyOptionsDto>;
public sealed record CompletePlatformPasskeyRegistrationCommand(Guid CeremonyId, JsonElement Credential, string? FriendlyName) : IRequest;

public sealed class BeginPlatformPasskeyLoginValidator : AbstractValidator<BeginPlatformPasskeyLoginCommand>
{
    public BeginPlatformPasskeyLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(500);
    }
}

public sealed class CompletePlatformPasskeyLoginValidator : AbstractValidator<CompletePlatformPasskeyLoginCommand>
{
    public CompletePlatformPasskeyLoginValidator()
    {
        RuleFor(x => x.CeremonyId).NotEmpty();
        RuleFor(x => x.Credential.ValueKind).NotEqual(JsonValueKind.Undefined);
    }
}

public sealed class BeginPlatformPasskeyRegistrationValidator : AbstractValidator<BeginPlatformPasskeyRegistrationCommand>
{
    public BeginPlatformPasskeyRegistrationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(500);
    }
}

public sealed class CompletePlatformPasskeyRegistrationValidator : AbstractValidator<CompletePlatformPasskeyRegistrationCommand>
{
    public CompletePlatformPasskeyRegistrationValidator()
    {
        RuleFor(x => x.CeremonyId).NotEmpty();
        RuleFor(x => x.Credential.ValueKind).NotEqual(JsonValueKind.Undefined);
        RuleFor(x => x.FriendlyName).MaximumLength(120);
    }
}

public sealed class BeginPlatformPasskeyLoginCommandHandler : IRequestHandler<BeginPlatformPasskeyLoginCommand, PasskeyCeremonyOptionsDto>
{
    private readonly IApplicationDbContext _context; private readonly IIdentityPasskeyService _passkeys;
    public BeginPlatformPasskeyLoginCommandHandler(IApplicationDbContext context, IIdentityPasskeyService passkeys) => (_context, _passkeys) = (context, passkeys);
    public async Task<PasskeyCeremonyOptionsDto> Handle(BeginPlatformPasskeyLoginCommand request, CancellationToken ct)
    {
        var normalized = IdentityEmailAddress.Normalize(request.Email);
        var user = await _context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId && x.IdentityAccountId != null && !x.IsDeleted && x.IsActive &&
            x.Email.ToUpper() == normalized, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials");
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == user.IdentityAccountId!.Value && x.NormalizedEmail == normalized && x.IsActive && x.EmailVerifiedAt != null, ct);
        if (identity is null) throw new UnauthorizedException("Invalid credentials");
        return await _passkeys.BeginSignInAsync(identity.Email, ct);
    }
}

public sealed class CompletePlatformPasskeyLoginCommandHandler : IRequestHandler<CompletePlatformPasskeyLoginCommand, AuthResponseDto>
{
    private readonly IIdentityPasskeyService _passkeys; private readonly IPlatformSessionIssuer _issuer;
    public CompletePlatformPasskeyLoginCommandHandler(IIdentityPasskeyService passkeys, IPlatformSessionIssuer issuer) => (_passkeys, _issuer) = (passkeys, issuer);
    public async Task<AuthResponseDto> Handle(CompletePlatformPasskeyLoginCommand request, CancellationToken ct)
        => await _issuer.IssueAsync(await _passkeys.CompleteSignInAsync(request.CeremonyId, request.Credential, ct), ct);
}

public sealed class BeginPlatformPasskeyRegistrationCommandHandler : IRequestHandler<BeginPlatformPasskeyRegistrationCommand, PasskeyCeremonyOptionsDto>
{
    private readonly IApplicationDbContext _context; private readonly IIdentityPasskeyService _passkeys;
    public BeginPlatformPasskeyRegistrationCommandHandler(IApplicationDbContext context, IIdentityPasskeyService passkeys) => (_context, _passkeys) = (context, passkeys);
    public async Task<PasskeyCeremonyOptionsDto> Handle(BeginPlatformPasskeyRegistrationCommand request, CancellationToken ct)
    {
        var normalized = IdentityEmailAddress.Normalize(request.Email);
        var user = await _context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == PlatformConstants.PlatformTenantId &&
            x.IdentityAccountId != null && !x.IsDeleted && x.IsActive && x.Email.ToUpper() == normalized, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) throw new UnauthorizedException("Invalid credentials");
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == user.IdentityAccountId!.Value && x.NormalizedEmail == normalized && x.IsActive && x.EmailVerifiedAt != null, ct);
        if (identity is null) throw new UnauthorizedException("Invalid credentials");
        return await _passkeys.BeginRegistrationAsync(identity.Id, ct);
    }
}

public sealed class CompletePlatformPasskeyRegistrationCommandHandler : IRequestHandler<CompletePlatformPasskeyRegistrationCommand>
{
    private readonly IApplicationDbContext _context; private readonly IIdentityPasskeyService _passkeys;
    public CompletePlatformPasskeyRegistrationCommandHandler(IApplicationDbContext context, IIdentityPasskeyService passkeys) => (_context, _passkeys) = (context, passkeys);
    public async Task Handle(CompletePlatformPasskeyRegistrationCommand request, CancellationToken ct)
    {
        var identityId = await _context.IdentityPasskeyCeremonies.Where(x => x.Id == request.CeremonyId && x.Purpose == Domain.Enums.IdentityPasskeyCeremonyPurpose.Registration)
            .Select(x => (Guid?)x.IdentityAccountId).SingleOrDefaultAsync(ct) ?? throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        var isPlatformIdentity = await _context.Users.IgnoreQueryFilters().AnyAsync(x => x.TenantId == PlatformConstants.PlatformTenantId &&
            x.IdentityAccountId == identityId && !x.IsDeleted && x.IsActive, ct);
        if (!isPlatformIdentity) throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        await _passkeys.CompleteRegistrationAsync(identityId, request.CeremonyId, request.Credential, request.FriendlyName, ct);
    }
}
