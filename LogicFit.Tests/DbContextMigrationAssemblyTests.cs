using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class DbContextMigrationAssemblyTests
{
    [Fact]
    public void Platform_and_tenant_use_distinct_migration_histories_and_assemblies()
    {
        Assert.NotEqual(PlatformDbContext.MigrationsAssemblyName, TenantDbContext.MigrationsAssemblyName);
        Assert.NotEqual(PlatformDbContext.MigrationHistoryTable, TenantDbContext.MigrationHistoryTable);
        Assert.Contains("Platform", PlatformDbContext.MigrationsAssemblyName);
        Assert.Contains("Tenant", TenantDbContext.MigrationsAssemblyName);
    }

    [Fact]
    public void Ownership_overlap_is_limited_to_declared_shared_contracts()
    {
        var overlap = DbContextOwnership.PlatformEntities
            .Intersect(DbContextOwnership.TenantEntities)
            .ToHashSet();

        Assert.True(
            overlap.SetEquals(DbContextOwnership.SharedContractEntities),
            $"Unexpected overlap: {string.Join(", ", overlap.Select(type => type.Name))}");
    }

    [Fact]
    public void Each_context_discovers_only_its_dedicated_baseline()
    {
        using var platform = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformMigrationValidation;Trusted_Connection=True;")
            .Options);
        using var tenant = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitTenantMigrationValidation;Trusted_Connection=True;")
            .Options, Guid.Parse("2f0b6d1f-2d8f-4f32-b7d0-2cbde6afbf91"));

        var platformMigrations = platform.Database.GetMigrations().ToArray();
        var tenantMigrations = tenant.Database.GetMigrations().ToArray();

        Assert.Contains(platformMigrations, id => id.EndsWith("_PlatformBaseline", StringComparison.Ordinal));
        Assert.DoesNotContain(platformMigrations, id => id.EndsWith("_TenantBaseline", StringComparison.Ordinal));
        Assert.Contains(tenantMigrations, id => id.EndsWith("_TenantBaseline", StringComparison.Ordinal));
        Assert.DoesNotContain(tenantMigrations, id => id.EndsWith("_PlatformBaseline", StringComparison.Ordinal));
    }
}
