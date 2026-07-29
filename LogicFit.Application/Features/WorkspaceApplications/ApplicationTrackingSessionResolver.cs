using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications;

internal static class ApplicationTrackingSessionResolver
{
    public static async Task<ApplicationTrackingSession> GetActiveAsync(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new UnauthorizedException("Invalid application tracking session.");

        var session = await context.ApplicationTrackingSessions
            .Include(x => x.ApplicationRequest)
            .FirstOrDefaultAsync(x => x.TokenHash == ApplicationTrackingToken.Hash(rawToken), cancellationToken);

        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= dateTimeService.UtcNow)
            throw new UnauthorizedException("Application tracking session has expired.");

        session.LastUsedAt = dateTimeService.UtcNow;
        return session;
    }
}
