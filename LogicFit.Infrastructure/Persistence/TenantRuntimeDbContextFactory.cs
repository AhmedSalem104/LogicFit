using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Builds a tenant context from a server-resolved mapping.  This is used by request routing and
/// by controlled background jobs; it never accepts a database name or connection string from a
/// client-facing command.
/// </summary>
public static class TenantRuntimeDbContextFactory
{
    public static TenantDbContext Create(TenantDatabaseResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        if (resolution.TenantId == Guid.Empty)
            throw new ArgumentException("A non-empty tenant id is required.", nameof(resolution));

        var options = new DbContextOptionsBuilder<TenantDbContext>();
        DbContextSqlServerOptions.UseTenantDatabase(options, resolution.ConnectionString);
        return new TenantDbContext(options.Options, resolution.TenantId);
    }
}
