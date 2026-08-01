using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Auth.Commands.PlatformOtpLogin;

public sealed record RequestPlatformLoginOtpCommand(string Email, string Password, string? SessionBinding) : IRequest<OtpChallengeDto>;
public sealed record VerifyPlatformLoginOtpCommand(Guid ChallengeId, string Code, string? SessionBinding) : IRequest<AuthResponseDto>;

public sealed class RequestPlatformLoginOtpValidator : AbstractValidator<RequestPlatformLoginOtpCommand>
{
    public RequestPlatformLoginOtpValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(512);
    }
}

public sealed class VerifyPlatformLoginOtpValidator : AbstractValidator<VerifyPlatformLoginOtpCommand>
{
    public VerifyPlatformLoginOtpValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}

public sealed class RequestPlatformLoginOtpHandler : IRequestHandler<RequestPlatformLoginOtpCommand, OtpChallengeDto>
{
    private readonly IApplicationDbContext _db; private readonly IOtpService _otp; private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _currentUser;
    public RequestPlatformLoginOtpHandler(IApplicationDbContext db, IOtpService otp, IDateTimeService clock,
        ICurrentUserService currentUser)
        => (_db, _otp, _clock, _currentUser) = (db, otp, clock, currentUser);
    public async Task<OtpChallengeDto> Handle(RequestPlatformLoginOtpCommand request, CancellationToken ct)
    {
        var email = IdentityEmailAddress.Normalize(request.Email);
        var user = await _db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId && x.Email != null &&
            x.Email.ToUpper() == email && x.IsActive && !x.IsDeleted &&
            (x.Role == UserRole.PlatformOwner || x.Role == UserRole.PlatformAdmin), ct);
        if (user is null || !user.IdentityAccountId.HasValue)
        {
            SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginFailed", false,
                tenantId: PlatformConstants.PlatformTenantId);
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid credentials");
        }
        var identity = await _db.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == user.IdentityAccountId.Value && x.IsActive, ct);
        if (identity?.PhoneVerifiedAt is null || identity.EmailVerifiedAt is null ||
            string.IsNullOrWhiteSpace(identity.NormalizedPhoneNumber) || identity.LockoutEndUtc > _clock.UtcNow)
        {
            SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginFailed", false,
                identity?.Id, PlatformConstants.PlatformTenantId);
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid credentials");
        }
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            identity.FailedLoginAttempts++;
            if (identity.FailedLoginAttempts >= 5)
            {
                identity.FailedLoginAttempts = 0;
                identity.LockoutEndUtc = _clock.UtcNow.AddMinutes(15);
            }
            SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginFailed", false,
                identity.Id, PlatformConstants.PlatformTenantId);
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid credentials");
        }
        identity.FailedLoginAttempts = 0;
        identity.LockoutEndUtc = null;
        SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginSucceeded", true,
            identity.Id, PlatformConstants.PlatformTenantId);
        await _db.SaveChangesAsync(ct);
        return await _otp.RequestAsync(identity.NormalizedPhoneNumber, OtpPurpose.PlatformAdminLogin, identity.Id, request.SessionBinding, ct);
    }
}

public sealed class VerifyPlatformLoginOtpHandler : IRequestHandler<VerifyPlatformLoginOtpCommand, AuthResponseDto>
{
    private readonly IOtpService _otp; private readonly IPlatformSessionIssuer _issuer;
    public VerifyPlatformLoginOtpHandler(IOtpService otp, IPlatformSessionIssuer issuer) => (_otp, _issuer) = (otp, issuer);
    public async Task<AuthResponseDto> Handle(VerifyPlatformLoginOtpCommand request, CancellationToken ct)
    {
        var challenge = await _otp.VerifyAsync(request.ChallengeId, request.Code, OtpPurpose.PlatformAdminLogin, request.SessionBinding, ct);
        if (!challenge.IdentityAccountId.HasValue) throw new UnauthorizedException("Invalid credentials");
        return await _issuer.IssueAsync(challenge.IdentityAccountId.Value, ct);
    }
}
