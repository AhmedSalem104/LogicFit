using LogicFit.API.Features.DayPasses;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class DayPassFeatureContractTests
{
    private static readonly Guid TenantId = Guid.Parse("2f0b6d1f-2d8f-4f32-b7d0-2cbde6afbf91");

    [Fact]
    public void Daily_pass_permission_is_tenant_scoped_and_owner_compatible()
    {
        Assert.Contains(Permissions.ManageDayPasses, Permissions.TenantPermissions);
        Assert.DoesNotContain(Permissions.ManageDayPasses, Permissions.PlatformPermissions);
    }

    [Fact]
    public void Tenant_model_contains_daily_pass_tables_and_platform_model_does_not()
    {
        using var tenant = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitTenantModelValidation;Trusted_Connection=True;")
            .Options, TenantId);
        using var platform = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformModelValidation;Trusted_Connection=True;")
            .Options);

        var tenantTables = tenant.Model.GetEntityTypes().Select(x => x.GetTableName()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var platformTables = platform.Model.GetEntityTypes().Select(x => x.GetTableName()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("DayPassTypes", tenantTables);
        Assert.Contains("DayPassSales", tenantTables);
        Assert.DoesNotContain("DayPassTypes", platformTables);
        Assert.DoesNotContain("DayPassSales", platformTables);
    }

    [Fact]
    public void Daily_pass_controller_requires_permission_and_gym_capability()
    {
        var policies = typeof(DayPassesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Select(x => x.Policy)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(Permissions.ManageDayPasses, policies);
        Assert.Contains(WorkspaceCapabilities.GymExperience, policies);
    }

    [Fact]
    public void Other_payment_method_is_supported_for_top_gym_compatibility()
    {
        Assert.Equal(4, (int)PaymentMethod.Other);
    }
}
