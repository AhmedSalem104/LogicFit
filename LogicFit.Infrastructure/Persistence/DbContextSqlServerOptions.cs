using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Centralizes the migration-history boundary.  Provisioning code must use these methods instead
/// of accepting a migration assembly or database name from an HTTP request.
/// </summary>
public static class DbContextSqlServerOptions
{
    public static void UsePlatformDatabase(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(PlatformDbContext.MigrationsAssemblyName)
            .MigrationsHistoryTable(PlatformDbContext.MigrationHistoryTable));
    }

    public static void UseTenantDatabase(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(TenantDbContext.MigrationsAssemblyName)
            .MigrationsHistoryTable(TenantDbContext.MigrationHistoryTable));
    }
}
