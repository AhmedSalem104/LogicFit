using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity.DTOs;
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
    private readonly LegacyIdentityMigrationService _legacyMigration;

    public IdentitySignInCommandHandler(
        IApplicationDbContext context,
        IIdentityWorkspaceSessionIssuer issuer,
        IDateTimeService clock,
        ICurrentUserService currentUser,
        LegacyIdentityMigrationService legacyMigration)
    {
        _context = context;
        _issuer = issuer;
        _clock = clock;
        _currentUser = currentUser;
        _legacyMigration = legacyMigration;
    }

    public async Task<IdentitySignInDto> Handle(IdentitySignInCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityEmailAddress.Normalize(request.Email);
        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        var migratedLegacyAccount = false;
        if (identity is null)
        {
            identity = await _legacyMigration.TryMigrateAsync(normalizedEmail, request.Password, cancellationToken);
            migratedLegacyAccount = identity is not null;
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
        if (migratedLegacyAccount)
            SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityLegacyAccountMigrated", true, identity.Id);
        SecurityAuditLog.Add(_context, _currentUser, _clock, "IdentityPasswordLoginSucceeded", true, identity.Id);
        await _context.SaveChangesAsync(cancellationToken);

        return await _issuer.IssueAsync(identity.Id, cancellationToken);
    }
}
