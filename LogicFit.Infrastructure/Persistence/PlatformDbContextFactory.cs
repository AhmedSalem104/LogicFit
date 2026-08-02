using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogicFit.Infrastructure.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGICFIT_PLATFORM_EF_CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformDesignTime;Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(PlatformDbContext.MigrationHistoryTable))
            .Options;
        return new PlatformDbContext(options);
    }
}
