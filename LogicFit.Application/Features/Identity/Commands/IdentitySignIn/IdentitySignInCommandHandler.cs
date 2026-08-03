using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.IdentitySignIn;

public sealed class IdentitySignInCommandHandler : IRequestHandler<IdentitySignInCommand, IdentitySignInDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityWorkspaceSessionIssuer _issuer;
    private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _currentUser;

    public IdentitySignInCommandHandler(
        IApplicationDbContext context,
        IIdentityWorkspaceSessionIssuer issuer,
        IDateTimeService clock,
        ICurrentUserService currentUser)
    {
        _context = context;
        _issuer = issuer;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<IdentitySignInDto> Handle(IdentitySignInCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityEmailAddress.Normalize(request.Email);
        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        // Platform tenant creation existed before the identity-first flow and may have left
        // an owner as a tenant-local User without an IdentityAccount. Migrate that account only
        // after the caller proves the existing local password; this repairs old tenants without
        // exposing a password-reset or account-enumeration path.
        if (identity is null)
        {
            identity = await TryLinkLegacyTenantUserAsync(normalizedEmail, request.Email, request.Password, cancellationToken);
        }

        if (identity is null || !identity.IsActive || identity.EmailVerifiedAt is null)
        {
            SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityPasswordLoginFailed", false, identity?.Id);
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials");
        }
        if (identity.LockoutEndUtc > _clock.UtcNow)
        {
            SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityPasswordLoginFailed", false, identity.Id);
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials");
        }
        if (!BCrypt.Net.BCrypt.Verify(request.Password, identity.PasswordHash))
        {
            identity.FailedLoginAttempts++;
            if (identity.FailedLoginAttempts >= 5)
            {
                identity.LockoutEndUtc = _clock.UtcNow.AddMinutes(15);
                identity.FailedLoginAttempts = 0;
            }
            SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityPasswordLoginFailed", false, identity.Id);
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials");
        }

        identity.FailedLoginAttempts = 0;
        identity.LockoutEndUtc = null;
        SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityPasswordLoginSucceeded", true, identity.Id);
        await _context.SaveChangesAsync(cancellationToken);

        return await _issuer.IssueAsync(identity.Id, cancellationToken);
    }

    private async Task<IdentityAccount?> TryLinkLegacyTenantUserAsync(
        string normalizedEmail,
        string requestedEmail,
        string password,
        CancellationToken cancellationToken)
    {
        var legacyUsers = await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Profile)
            .Include(x => x.Tenant)
            .Where(x => x.IdentityAccountId == null &&
                        x.IsActive &&
                        !x.IsDeleted &&
                        x.TenantId != PlatformConstants.PlatformTenantId &&
                        !x.Tenant.IsDeleted &&
                        x.Email.ToUpper() == normalizedEmail)
            .ToListAsync(cancellationToken);

        var matchingUsers = legacyUsers
            .Where(x => BCrypt.Net.BCrypt.Verify(password, x.PasswordHash))
            .ToList();
        if (matchingUsers.Count != 1)
        {
            return null;
        }

        var user = matchingUsers[0];
        var now = _clock.UtcNow;
        var identity = new IdentityAccount
        {
            FullName = user.Profile?.FullName ?? user.Email,
            Email = requestedEmail.Trim(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = user.PhoneNumber,
            PasswordHash = user.PasswordHash,
            IsActive = true,
            EmailVerifiedAt = now
        };
        _context.IdentityAccounts.Add(identity);
        user.IdentityAccountId = identity.Id;

        var membershipStatus = user.Tenant.Status is TenantStatus.Active or TenantStatus.Trial or TenantStatus.PastDue
            ? WorkspaceMembershipStatus.Active
            : WorkspaceMembershipStatus.PendingPlatformApproval;
        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            IdentityAccountId = identity.Id,
            TenantId = user.TenantId,
            UserId = user.Id,
            Role = user.Role,
            Status = membershipStatus,
            ApprovedAt = membershipStatus == WorkspaceMembershipStatus.Active ? now : null,
            ApprovedBy = membershipStatus == WorkspaceMembershipStatus.Active ? "legacy-account-migration" : null
        });

        SecurityAuditLog.Add(_context, _currentUser, _clock, "LegacyTenantUserLinkedToIdentity", true, identity.Id);
        await _context.SaveChangesAsync(cancellationToken);
        return identity;
    }
}
