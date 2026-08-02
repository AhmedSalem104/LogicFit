using System.Security.Cryptography;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogicFit.Infrastructure.Services;

public sealed class SensitiveActionGrantService(
    IApplicationDbContext context,
    IDateTimeService clock,
    ICurrentUserService currentUser,
    IConfiguration configuration) : ISensitiveActionGrantService
{
    public async Task<SensitiveActionGrantDto> ReauthenticateAsync(
        Guid userId,
        Guid? tenantId,
        string currentPassword,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(scope))
            throw new UnauthorizedException("Reauthentication is required.");

        var user = await context.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive &&
                (!tenantId.HasValue || x.TenantId == tenantId.Value), cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(currentPassword) ||
            !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            SecurityAuditLog.Add(context, currentUser, clock, "SensitiveActionReauthenticationFailed", false, userId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Current password is incorrect.");
        }

        var now = clock.UtcNow;
        var lifetimeMinutes = Math.Clamp(configuration.GetValue("Backup:SensitiveGrantMinutes", 5), 1, 10);
        var expiresAt = now.AddMinutes(lifetimeMinutes);
        var active = await context.SensitiveActionGrants
            .Where(x => x.UserId == userId && x.TenantId == tenantId && x.Scope == scope &&
                x.ConsumedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        foreach (var grant in active)
            grant.RevokedAtUtc = now;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.SensitiveActionGrants.Add(new SensitiveActionGrant
        {
            UserId = userId,
            TenantId = tenantId,
            Scope = scope,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAt,
            CreatedByIp = currentUser.IpAddress
        });
        SecurityAuditLog.Add(context, currentUser, clock, "SensitiveActionReauthenticationSucceeded", true, userId, tenantId);
        await context.SaveChangesAsync(cancellationToken);
        return new SensitiveActionGrantDto(rawToken, expiresAt, scope);
    }

    public async Task ConsumeAsync(
        string rawGrantToken,
        Guid userId,
        Guid? tenantId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawGrantToken))
            throw new UnauthorizedException("A valid sensitive-action grant is required.");

        var now = clock.UtcNow;
        var hash = Hash(rawGrantToken);
        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        var grant = await context.SensitiveActionGrants
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.UserId == userId &&
                x.TenantId == tenantId && x.Scope == scope, cancellationToken);
        if (grant is null || grant.ConsumedAtUtc.HasValue || grant.RevokedAtUtc.HasValue || grant.ExpiresAtUtc <= now)
            throw new UnauthorizedException("The sensitive-action grant is invalid or expired.");

        grant.ConsumedAtUtc = now;
        SecurityAuditLog.Add(context, currentUser, clock, "SensitiveActionGrantConsumed", true, userId, tenantId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedException("The sensitive-action grant has already been consumed.");
        }
    }

    private static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
