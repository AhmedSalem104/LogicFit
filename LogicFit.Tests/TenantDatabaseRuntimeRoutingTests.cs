using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantDatabaseRuntimeRoutingTests
{
    [Fact]
    public void Tenant_owned_sets_are_backed_by_the_resolved_tenant_context()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = new StubTenantService { CurrentTenantId = tenantId };
        var requestScope = new TenantDatabaseRequestScope();
        requestScope.Set(new TenantDatabaseResolution(
            tenantId,
            Guid.NewGuid(),
            "test",
            "tenant-db",
            "Server=(localdb)\\MSSQLLocalDB;Database=TenantRoutingTest;Trusted_Connection=True;TrustServerCertificate=True;",
            TenantDbContext.MigrationsAssemblyName,
            DateTime.UtcNow));

        using var platform = CreatePlatformContext();
        using var legacy = new ApplicationDbContextFactory().CreateDbContext([]);
        using var tenantAccessor = new TenantDatabaseContextAccessor(requestScope);
        var context = TenantAwareApplicationDbContextProxy.Create(
            platform,
            legacy,
            requestScope,
            tenantAccessor,
            tenantService);

        var currentContext = GetCurrentContext(context.Exercises);

        Assert.Same(tenantAccessor.Current, currentContext);
        Assert.Equal(tenantId, tenantAccessor.Current!.TenantId);
    }

    [Fact]
    public void Platform_owned_sets_stay_on_platform_even_inside_a_tenant_request()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = new StubTenantService { CurrentTenantId = tenantId };
        var requestScope = new TenantDatabaseRequestScope();
        requestScope.Set(new TenantDatabaseResolution(
            tenantId,
            Guid.NewGuid(),
            "test",
            "tenant-db",
            "Server=(localdb)\\MSSQLLocalDB;Database=TenantRoutingTest;Trusted_Connection=True;TrustServerCertificate=True;",
            TenantDbContext.MigrationsAssemblyName,
            null));

        using var platform = CreatePlatformContext();
        using var legacy = new ApplicationDbContextFactory().CreateDbContext([]);
        using var tenantAccessor = new TenantDatabaseContextAccessor(requestScope);
        var context = TenantAwareApplicationDbContextProxy.Create(
            platform,
            legacy,
            requestScope,
            tenantAccessor,
            tenantService);

        var currentContext = GetCurrentContext(context.TenantSubscriptions);

        Assert.Same(platform, currentContext);
    }

    [Fact]
    public void A_tenant_id_without_a_mapping_cannot_fall_back_to_the_shared_context()
    {
        var tenantService = new StubTenantService { CurrentTenantId = Guid.NewGuid() };
        var requestScope = new TenantDatabaseRequestScope();
        using var platform = CreatePlatformContext();
        using var legacy = new ApplicationDbContextFactory().CreateDbContext([]);
        using var tenantAccessor = new TenantDatabaseContextAccessor(requestScope);
        var context = TenantAwareApplicationDbContextProxy.Create(
            platform,
            legacy,
            requestScope,
            tenantAccessor,
            tenantService);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = context.Exercises);

        Assert.Contains("shared database fallback is disabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Tenant_query_filters_are_not_reused_between_workspaces()
    {
        var gymId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var freelanceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        using var gym = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=GymIsolationModel;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options, gymId);
        using var freelance = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FreelanceIsolationModel;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options, freelanceId);

        var gymFilter = gym.Model.FindEntityType(typeof(User))!.GetQueryFilter()!;
        var freelanceFilter = freelance.Model.FindEntityType(typeof(User))!.GetQueryFilter()!;
        var gymPredicate = gymFilter.Compile();
        var freelancePredicate = freelanceFilter.Compile();

        Assert.NotSame(gym.Model, freelance.Model);
        Assert.True((bool)gymPredicate.DynamicInvoke(new User { TenantId = gymId })!);
        Assert.False((bool)gymPredicate.DynamicInvoke(new User { TenantId = freelanceId })!);
        Assert.True((bool)freelancePredicate.DynamicInvoke(new User { TenantId = freelanceId })!);
        Assert.False((bool)freelancePredicate.DynamicInvoke(new User { TenantId = gymId })!);
    }

    [Fact]
    public void Tenant_context_rejects_cross_workspace_role_assignments_before_database_io()
    {
        var tenantId = Guid.NewGuid();
        using var context = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TenantWriteBoundary;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options, tenantId);

        context.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        });

        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    private static PlatformDbContext CreatePlatformContext()
        => new(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PlatformRoutingTest;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

    private static DbContext GetCurrentContext(object dbSet)
        => ((IInfrastructure<IServiceProvider>)dbSet)
            .Instance
            .GetRequiredService<ICurrentDbContext>()
            .Context;

    private sealed class StubTenantService : ITenantService
    {
        public Guid? CurrentTenantId { get; set; }

        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(true);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }
}
