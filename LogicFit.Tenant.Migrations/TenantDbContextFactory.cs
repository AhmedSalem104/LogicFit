using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogicFit.TenantDatabase.Migrations;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantMigrationDbContext>
{
    private static readonly Guid DesignTimeTenantId = Guid.Parse("2f0b6d1f-2d8f-4f32-b7d0-2cbde6afbf91");

    public TenantMigrationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGICFIT_TENANT_EF_CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=LogicFitTenantDesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<TenantMigrationDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(TenantDbContext.MigrationHistoryTable))
            .Options;

        return new TenantMigrationDbContext(options);
    }
}
