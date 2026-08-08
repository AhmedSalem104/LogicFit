using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

public class RefreshTokenService : IRefreshTokenService
{
    // Kept in code (rather than config) to avoid an Application-layer dependency on IConfiguration.
    public const int RefreshTokenExpiryDays = 7;

    public const string SurfaceTenant = "tenant";
    public const string SurfacePlatform = "platform";

    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTimeService;

    public RefreshTokenService(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
    }

    public RefreshToken Issue(User user, string? ipAddress, string surface)
    {
        var token = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId == Guid.Empty ? null : user.TenantId,
            Surface = surface,
            Token = _jwtService.GenerateRefreshToken(),
            ExpiresAt = _dateTimeService.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = _dateTimeService.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(token);
        return token;
    }

    public async Task<(User user, RefreshToken newToken)> RotateAsync(string token, string? ipAddress, string expectedSurface, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existing == null || existing.Surface != expectedSurface)
        {
            throw new UnauthorizedException("Invalid or expired refresh token");
        }

        if (existing.RevokedAt.HasValue && !string.IsNullOrWhiteSpace(existing.ReplacedByToken))
        {
            // A rotated token was presented again. Treat the entire token family as compromised.
            await RevokeAllAsync(existing.UserId, ipAddress, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("REFRESH_TOKEN_REUSE_DETECTED");
        }

        // Use the injected clock consistently with Issue(), otherwise a fixed/test clock can
        // disagree with RefreshToken.IsActive's wall-clock comparison and reject valid tokens.
        if (existing.RevokedAt.HasValue || existing.ExpiresAt <= _dateTimeService.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token");

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == existing.UserId && !u.IsDeleted, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Account is unavailable");
        }

        var replacement = Issue(user, ipAddress, expectedSurface);

        existing.RevokedAt = _dateTimeService.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReplacedByToken = replacement.Token;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new UnauthorizedException("REFRESH_TOKEN_REUSE_DETECTED");
        }

        return (user, replacement);
    }

    public async Task RevokeAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > _dateTimeService.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var t in active)
        {
            t.RevokedAt = _dateTimeService.UtcNow;
            t.RevokedByIp = ipAddress;
        }
    }
}
