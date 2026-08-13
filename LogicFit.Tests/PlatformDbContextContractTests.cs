using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformDbContextContractTests
{
    [Fact]
    public void Constructor_uses_the_context_specific_options_type()
    {
        var constructor = typeof(PlatformDbContext).GetConstructor(
            new[] { typeof(DbContextOptions<PlatformDbContext>) });

        Assert.NotNull(constructor);
    }

    [Fact]
    public void Platform_membership_model_does_not_map_tenant_user_navigation()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformModelValidation;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new PlatformDbContext(options);

        var membershipType = context.Model.FindEntityType(typeof(LogicFit.Domain.Entities.WorkspaceMembership));

        Assert.NotNull(membershipType);
        Assert.Null(membershipType!.FindNavigation(nameof(LogicFit.Domain.Entities.WorkspaceMembership.User)));
    }

    [Fact]
    public void Platform_membership_query_uses_only_platform_owned_navigation()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitPlatformModelValidation;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new PlatformDbContext(options);

        var query = context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Include(x => x.Tenant)
            .Where(x => x.IdentityAccountId != Guid.Empty && !x.Tenant.IsDeleted);

        var sql = query.ToQueryString();

        Assert.Contains("WorkspaceMemberships", sql);
        Assert.Contains("Tenants", sql);
        Assert.DoesNotContain("Users", sql);
    }
}
