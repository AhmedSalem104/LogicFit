using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity;

internal static class IdentityWorkspaceSessionResolver
{
    public static async Task<IdentityWorkspaceSession> GetActiveAsync(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException("Invalid identity session.");

        var session = await context.IdentityWorkspaceSessions
            .FirstOrDefaultAsync(x => x.TokenHash == IdentityWorkspaceSessionToken.Hash(token), cancellationToken);
        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= dateTimeService.UtcNow)
            throw new UnauthorizedException("Identity session has expired.");

        session.LastUsedAt = dateTimeService.UtcNow;
        return session;
    }
}
