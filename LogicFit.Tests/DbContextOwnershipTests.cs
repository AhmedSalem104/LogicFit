using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class DbContextOwnershipTests
{
    private static readonly Guid TenantId = Guid.Parse("2f0b6d1f-2d8f-4f32-b7d0-2cbde6afbf91");

    [Fact]
    public void Platform_context_does_not_model_tenant_operational_tables()
    {
        using var context = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitModelValidation;Trusted_Connection=True;")
            .Options);

        var tables = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(table => table is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Tenants", tables);
        Assert.Contains("IdentityAccounts", tables);
        Assert.DoesNotContain("DomainUsers", tables);
        Assert.DoesNotContain("WorkoutPrograms", tables);
        Assert.DoesNotContain("Appointments", tables);
    }

    [Fact]
    public void Tenant_context_does_not_model_platform_tables_and_requires_scope()
    {
        using var context = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitTenantModelValidation;Trusted_Connection=True;")
            .Options, TenantId);

        var tables = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(table => table is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("DomainUsers", tables);
        Assert.Contains("WorkoutPrograms", tables);
        Assert.DoesNotContain("IdentityAccounts", tables);
        Assert.DoesNotContain("ApplicationRequests", tables);
        Assert.DoesNotContain("TenantSubscriptions", tables);
        Assert.Equal(TenantId, context.TenantId);
    }

    [Fact]
    public void Legacy_context_exposes_resource_pool_for_compatibility_migration()
    {
        using var context = new LogicFit.Infrastructure.Persistence.ApplicationDbContextFactory().CreateDbContext([]);
        var tables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("DatabaseResources", tables);
        Assert.Contains("TenantDatabaseMappings", tables);
    }
}
