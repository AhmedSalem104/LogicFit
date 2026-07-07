using LogicFit.Domain.Entities;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>Issues, rotates and revokes persisted refresh tokens.</summary>
public interface IRefreshTokenService
{
    /// <summary>Creates and persists a new refresh token for the user. Does not call SaveChanges.</summary>
    RefreshToken Issue(User user, string? ipAddress, string surface);

    /// <summary>
    /// Validates an incoming refresh token, revokes it (rotation) and issues a replacement.
    /// The token must have been issued on the same <paramref name="expectedSurface"/>.
    /// Returns the owning user plus the new token. Throws if invalid/expired/revoked/wrong surface.
    /// </summary>
    Task<(User user, RefreshToken newToken)> RotateAsync(string token, string? ipAddress, string expectedSurface, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active refresh token for the user (logout everywhere).</summary>
    Task RevokeAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);
}
