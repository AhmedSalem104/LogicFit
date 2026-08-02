using System.Security.Cryptography;
using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.Otp;

public sealed record RequestPhoneLoginOtpCommand(string PhoneNumber, string? SessionBinding) : IRequest<OtpChallengeDto>;
public sealed record VerifyPhoneLoginOtpCommand(Guid ChallengeId, string Code, string? SessionBinding) : IRequest<IdentitySignInDto>;
public sealed record RequestIdentityPhoneOtpCommand(string PhoneNumber, OtpPurpose Purpose, string? WorkspaceSelectionToken, string? SessionBinding) : IRequest<OtpChallengeDto>;
public sealed record VerifyIdentityPhoneOtpCommand(Guid ChallengeId, string Code, OtpPurpose Purpose, string? WorkspaceSelectionToken, string? SessionBinding) : IRequest;
public sealed record ResetPasswordWithPhoneOtpCommand(Guid ChallengeId, string Code, string NewPassword, string? SessionBinding) : IRequest;

public sealed class RequestPhoneLoginOtpValidator : AbstractValidator<RequestPhoneLoginOtpCommand>
{
    public RequestPhoneLoginOtpValidator() => RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(32);
}
public sealed class VerifyPhoneLoginOtpValidator : AbstractValidator<VerifyPhoneLoginOtpCommand>
{
    public VerifyPhoneLoginOtpValidator() { RuleFor(x => x.ChallengeId).NotEmpty(); RuleFor(x => x.Code).NotEmpty().MaximumLength(10); }
}
public sealed class RequestIdentityPhoneOtpValidator : AbstractValidator<RequestIdentityPhoneOtpCommand>
{
    public RequestIdentityPhoneOtpValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Purpose).Must(x => x is OtpPurpose.PhoneVerification or OtpPurpose.ChangePhone);
    }
}
public sealed class VerifyIdentityPhoneOtpValidator : AbstractValidator<VerifyIdentityPhoneOtpCommand>
{
    public VerifyIdentityPhoneOtpValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Purpose).Must(x => x is OtpPurpose.PhoneVerification or OtpPurpose.ChangePhone);
    }
}
public sealed class ResetPasswordWithPhoneOtpValidator : AbstractValidator<ResetPasswordWithPhoneOtpCommand>
{
    public ResetPasswordWithPhoneOtpValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]");
    }
}

public sealed class RequestPhoneLoginOtpHandler : IRequestHandler<RequestPhoneLoginOtpCommand, OtpChallengeDto>
{
    private readonly IApplicationDbContext _db; private readonly IOtpService _otp;
    public RequestPhoneLoginOtpHandler(IApplicationDbContext db, IOtpService otp) => (_db, _otp) = (db, otp);
    public async Task<OtpChallengeDto> Handle(RequestPhoneLoginOtpCommand request, CancellationToken ct)
    {
        var phone = OtpService.NormalizePhone(request.PhoneNumber);
        var identity = await _db.IdentityAccounts.SingleOrDefaultAsync(x =>
            x.NormalizedPhoneNumber == phone && x.EmailVerifiedAt != null && x.IsActive, ct);
        return await _otp.RequestAsync(phone, OtpPurpose.PasswordlessLogin, identity?.Id,
            request.SessionBinding, ct, identity is not null);
    }
}

public sealed class VerifyPhoneLoginOtpHandler : IRequestHandler<VerifyPhoneLoginOtpCommand, IdentitySignInDto>
{
    private readonly IApplicationDbContext _db; private readonly IOtpService _otp;
    private readonly IIdentityWorkspaceSessionIssuer _issuer; private readonly IDateTimeService _clock;
    public VerifyPhoneLoginOtpHandler(IApplicationDbContext db, IOtpService otp,
        IIdentityWorkspaceSessionIssuer issuer, IDateTimeService clock)
        => (_db, _otp, _issuer, _clock) = (db, otp, issuer, clock);
    public async Task<IdentitySignInDto> Handle(VerifyPhoneLoginOtpCommand request, CancellationToken ct)
    {
        var challenge = await _otp.VerifyAsync(request.ChallengeId, request.Code, OtpPurpose.PasswordlessLogin, request.SessionBinding, ct);
        if (!challenge.IdentityAccountId.HasValue) throw new UnauthorizedException("Invalid credentials");
        var identity = await _db.IdentityAccounts.SingleOrDefaultAsync(x =>
            x.Id == challenge.IdentityAccountId.Value && x.IsActive &&
            x.NormalizedPhoneNumber == challenge.NormalizedPhoneNumber, ct);
        if (identity is null) throw new UnauthorizedException("Invalid credentials");
        if (identity.PhoneVerifiedAt is null)
        {
            identity.PhoneVerifiedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return await _issuer.IssueAsync(challenge.IdentityAccountId.Value, ct);
    }
}

public sealed class RequestIdentityPhoneOtpHandler : IRequestHandler<RequestIdentityPhoneOtpCommand, OtpChallengeDto>
{
    private readonly IApplicationDbContext _db; private readonly ICurrentUserService _current; private readonly IDateTimeService _clock; private readonly IOtpService _otp;
    public RequestIdentityPhoneOtpHandler(IApplicationDbContext db, ICurrentUserService current, IDateTimeService clock, IOtpService otp)
        => (_db, _current, _clock, _otp) = (db, current, clock, otp);
    public async Task<OtpChallengeDto> Handle(RequestIdentityPhoneOtpCommand request, CancellationToken ct)
    {
        var identityId = await OtpIdentityResolver.ResolveAsync(_db, _current, _clock, request.WorkspaceSelectionToken, ct);
        return await _otp.RequestAsync(request.PhoneNumber, request.Purpose, identityId, request.SessionBinding, ct);
    }
}

public sealed class VerifyIdentityPhoneOtpHandler : IRequestHandler<VerifyIdentityPhoneOtpCommand>
{
    private readonly IApplicationDbContext _db; private readonly ICurrentUserService _current; private readonly IDateTimeService _clock;
    private readonly IOtpService _otp; private readonly IRefreshTokenService _refresh;
    public VerifyIdentityPhoneOtpHandler(IApplicationDbContext db, ICurrentUserService current, IDateTimeService clock,
        IOtpService otp, IRefreshTokenService refresh)
        => (_db, _current, _clock, _otp, _refresh) = (db, current, clock, otp, refresh);
    public async Task Handle(VerifyIdentityPhoneOtpCommand request, CancellationToken ct)
    {
        var identityId = await OtpIdentityResolver.ResolveAsync(_db, _current, _clock, request.WorkspaceSelectionToken, ct);
        var challenge = await _otp.VerifyAsync(request.ChallengeId, request.Code, request.Purpose, request.SessionBinding, ct);
        if (challenge.IdentityAccountId != identityId) throw new UnauthorizedException("OTP_INVALID");
        var identity = await _db.IdentityAccounts.SingleAsync(x => x.Id == identityId, ct);
        var duplicate = await _db.IdentityAccounts.AnyAsync(x => x.Id != identityId &&
            x.NormalizedPhoneNumber == challenge.NormalizedPhoneNumber, ct);
        if (duplicate) throw new ConflictException("PHONE_NUMBER_ALREADY_IN_USE");
        identity.PhoneNumber = challenge.NormalizedPhoneNumber;
        identity.NormalizedPhoneNumber = challenge.NormalizedPhoneNumber;
        identity.PhoneVerifiedAt = _clock.UtcNow;
        if (request.Purpose == OtpPurpose.ChangePhone)
        {
            var users = await _db.Users.IgnoreQueryFilters()
                .Where(x => x.IdentityAccountId == identityId && !x.IsDeleted).ToListAsync(ct);
            foreach (var user in users)
            {
                user.PermissionsVersion++;
                await _refresh.RevokeAllAsync(user.Id, _current.IpAddress, ct);
            }
            var sessions = await _db.IdentityWorkspaceSessions
                .Where(x => x.IdentityAccountId == identityId && x.RevokedAt == null).ToListAsync(ct);
            foreach (var session in sessions) session.RevokedAt = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}

public sealed record RequestPhonePasswordResetOtpCommand(string PhoneNumber, string? SessionBinding) : IRequest<OtpChallengeDto>;
public sealed class RequestPhonePasswordResetOtpHandler : IRequestHandler<RequestPhonePasswordResetOtpCommand, OtpChallengeDto>
{
    private readonly IApplicationDbContext _db; private readonly IOtpService _otp;
    public RequestPhonePasswordResetOtpHandler(IApplicationDbContext db, IOtpService otp) => (_db, _otp) = (db, otp);
    public async Task<OtpChallengeDto> Handle(RequestPhonePasswordResetOtpCommand request, CancellationToken ct)
    {
        var phone = OtpService.NormalizePhone(request.PhoneNumber);
        var identity = await _db.IdentityAccounts.SingleOrDefaultAsync(x =>
            x.NormalizedPhoneNumber == phone && x.PhoneVerifiedAt != null && x.IsActive, ct);
        return await _otp.RequestAsync(phone, OtpPurpose.PasswordReset, identity?.Id,
            request.SessionBinding, ct, identity is not null);
    }
}

public sealed class ResetPasswordWithPhoneOtpHandler : IRequestHandler<ResetPasswordWithPhoneOtpCommand>
{
    private readonly IApplicationDbContext _db; private readonly IOtpService _otp; private readonly IRefreshTokenService _refresh;
    private readonly ICurrentUserService _current; private readonly IDateTimeService _clock;
    public ResetPasswordWithPhoneOtpHandler(IApplicationDbContext db, IOtpService otp, IRefreshTokenService refresh,
        ICurrentUserService current, IDateTimeService clock) => (_db, _otp, _refresh, _current, _clock) = (db, otp, refresh, current, clock);
    public async Task Handle(ResetPasswordWithPhoneOtpCommand request, CancellationToken ct)
    {
        var challenge = await _otp.VerifyAsync(request.ChallengeId, request.Code, OtpPurpose.PasswordReset, request.SessionBinding, ct);
        if (!challenge.IdentityAccountId.HasValue) throw new UnauthorizedException("OTP_INVALID");
        var identity = await _db.IdentityAccounts.SingleAsync(x => x.Id == challenge.IdentityAccountId.Value, ct);
        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        identity.PasswordHash = hash;
        var users = await _db.Users.IgnoreQueryFilters().Where(x => x.IdentityAccountId == identity.Id && !x.IsDeleted).ToListAsync(ct);
        foreach (var user in users) { user.PasswordHash = hash; user.PermissionsVersion++; await _refresh.RevokeAllAsync(user.Id, _current.IpAddress, ct); }
        var sessions = await _db.IdentityWorkspaceSessions.Where(x => x.IdentityAccountId == identity.Id && x.RevokedAt == null).ToListAsync(ct);
        foreach (var session in sessions) session.RevokedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

internal static class OtpIdentityResolver
{
    public static async Task<Guid> ResolveAsync(IApplicationDbContext db, ICurrentUserService current, IDateTimeService clock,
        string? workspaceSelectionToken, CancellationToken ct)
    {
        if (Guid.TryParse(current.UserId, out var userId))
        {
            var linked = await db.Users.IgnoreQueryFilters().Where(x => x.Id == userId && x.IsActive && !x.IsDeleted)
                .Select(x => x.IdentityAccountId).SingleOrDefaultAsync(ct);
            if (linked.HasValue) return linked.Value;
        }
        if (!string.IsNullOrWhiteSpace(workspaceSelectionToken))
        {
            var session = await db.IdentityWorkspaceSessions.SingleOrDefaultAsync(x =>
                x.TokenHash == IdentityWorkspaceSessionToken.Hash(workspaceSelectionToken) &&
                x.RevokedAt == null && x.ExpiresAt > clock.UtcNow, ct);
            if (session is not null) return session.IdentityAccountId;
        }
        throw new UnauthorizedException("Identity authentication is required.");
    }
}
