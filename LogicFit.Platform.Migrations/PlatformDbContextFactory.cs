using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogicFit.Platform.Migrations;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformMigrationDbContext>
{
    public PlatformMigrationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGICFIT_PLATFORM_EF_CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformDesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<PlatformMigrationDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(PlatformDbContext.MigrationHistoryTable))
            .Options;

        return new PlatformMigrationDbContext(options);
    }
}
