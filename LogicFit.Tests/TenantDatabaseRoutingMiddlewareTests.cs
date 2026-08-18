using LogicFit.API.Middleware;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantDatabaseRoutingMiddlewareTests
{
    [Fact]
    public async Task Missing_mapping_returns_503_and_does_not_call_next()
    {
        var tenantService = new StubTenantService { CurrentTenantId = Guid.NewGuid() };
        var requestScope = new TenantDatabaseRequestScope();
        var nextCalled = false;
        var middleware = new TenantDatabaseRoutingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new TenantDatabaseRoutingOptions()),
            NullLogger<TenantDatabaseRoutingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(
            context,
            tenantService,
            new StubResolver(null),
            requestScope);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.False(nextCalled);
        Assert.Null(requestScope.Resolution);
    }

    [Fact]
    public async Task Resolved_mapping_is_available_during_next_and_cleared_afterward()
    {
        var tenantId = Guid.NewGuid();
        var resolution = new TenantDatabaseResolution(
            tenantId,
            Guid.NewGuid(),
            "test",
            "tenant-db",
            "Server=(localdb)\\MSSQLLocalDB;Database=TenantRoutingTest;Trusted_Connection=True;TrustServerCertificate=True;",
            TenantDbContext.MigrationsAssemblyName,
            null);
        var tenantService = new StubTenantService { CurrentTenantId = tenantId };
        var requestScope = new TenantDatabaseRequestScope();
        var nextCalled = false;
        var middleware = new TenantDatabaseRoutingMiddleware(
            _ =>
            {
                nextCalled = true;
                Assert.Same(resolution, requestScope.Resolution);
                return Task.CompletedTask;
            },
            Options.Create(new TenantDatabaseRoutingOptions()),
            NullLogger<TenantDatabaseRoutingMiddleware>.Instance);

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            tenantService,
            new StubResolver(resolution),
            requestScope);

        Assert.True(nextCalled);
        Assert.Null(requestScope.Resolution);
    }

    private sealed class StubResolver(TenantDatabaseResolution? resolution) : ITenantDatabaseResolver
    {
        public Task<TenantDatabaseResolution?> ResolveAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(resolution);
    }

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
