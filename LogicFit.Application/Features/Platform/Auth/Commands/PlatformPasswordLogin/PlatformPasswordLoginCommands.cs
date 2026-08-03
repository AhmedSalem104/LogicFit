using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Auth.Commands.PlatformPasswordLogin;

/// <summary>
/// Platform administration uses the same verified identity and password contract as the rest
/// of the product.  A platform session is issued only after the linked platform user and its
/// RBAC assignment have been validated by <see cref="IPlatformSessionIssuer"/>.
/// </summary>
public sealed record PlatformPasswordLoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

public sealed class PlatformPasswordLoginValidator : AbstractValidator<PlatformPasswordLoginCommand>
{
    public PlatformPasswordLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class PlatformPasswordLoginHandler : IRequestHandler<PlatformPasswordLoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPlatformSessionIssuer _issuer;
    private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _currentUser;

    public PlatformPasswordLoginHandler(
        IApplicationDbContext db,
        IPlatformSessionIssuer issuer,
        IDateTimeService clock,
        ICurrentUserService currentUser)
        => (_db, _issuer, _clock, _currentUser) = (db, issuer, clock, currentUser);

    public async Task<AuthResponseDto> Handle(PlatformPasswordLoginCommand request, CancellationToken cancellationToken)
    {
        var email = IdentityEmailAddress.Normalize(request.Email);
        var user = await _db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId &&
            x.IdentityAccountId.HasValue &&
            x.Email != null && x.Email.ToUpper() == email &&
            x.IsActive && !x.IsDeleted &&
            (x.Role == UserRole.PlatformOwner || x.Role == UserRole.PlatformAdmin),
            cancellationToken);

        var identity = user?.IdentityAccountId is Guid identityId
            ? await _db.IdentityAccounts.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == identityId, cancellationToken)
            : null;

        if (user is null || identity is null || !identity.IsActive || identity.EmailVerifiedAt is null ||
            identity.LockoutEndUtc > _clock.UtcNow || !BCrypt.Net.BCrypt.Verify(request.Password, identity.PasswordHash))
        {
            if (identity is not null)
            {
                identity.FailedLoginAttempts++;
                if (identity.FailedLoginAttempts >= 5)
                {
                    identity.FailedLoginAttempts = 0;
                    identity.LockoutEndUtc = _clock.UtcNow.AddMinutes(15);
                }
            }

            SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginFailed", false,
                identity?.Id, PlatformConstants.PlatformTenantId);
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials");
        }

        identity.FailedLoginAttempts = 0;
        identity.LockoutEndUtc = null;
        SecurityAuditLog.Add(_db, _currentUser, _clock, "PlatformPasswordLoginSucceeded", true,
            identity.Id, PlatformConstants.PlatformTenantId);
        await _db.SaveChangesAsync(cancellationToken);

        return await _issuer.IssueAsync(identity.Id, cancellationToken);
    }
}
