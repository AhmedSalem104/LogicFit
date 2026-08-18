using System.Security.Claims;
using LogicFit.API.Middleware;
using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantBoundarySecurityTests
{
    [Fact]
    public async Task Platform_token_is_rejected_by_tenant_api_even_when_header_supplies_a_tenant()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = new FakeTenantService(tenantId);
        var (context, nextCalled) = await InvokeAsync(
            "/api/clients",
            Principal("LogicFitPlatform"),
            tenantService,
            tenantId.ToString());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled);
        Assert.Null(tenantService.CurrentTenantId);
    }

    [Fact]
    public async Task Tenant_api_rejects_authenticated_request_without_tenant_claim()
    {
        var tenantService = new FakeTenantService();
        var (context, nextCalled) = await InvokeAsync(
            "/api/clients",
            Principal("LogicFitUsers"),
            tenantService);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Tenant_api_rejects_header_that_does_not_match_signed_tenant_claim()
    {
        var tokenTenantId = Guid.NewGuid();
        var headerTenantId = Guid.NewGuid();
        var tenantService = new FakeTenantService(tokenTenantId, headerTenantId);
        var (context, nextCalled) = await InvokeAsync(
            "/api/clients",
            Principal("LogicFitUsers", tokenTenantId),
            tenantService,
            headerTenantId.ToString());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled);
        Assert.Null(tenantService.CurrentTenantId);
    }

    [Fact]
    public async Task Tenant_api_rejects_signed_tenant_that_does_not_exist()
    {
        var tenantId = Guid.NewGuid();
        var (context, nextCalled) = await InvokeAsync(
            "/api/clients",
            Principal("LogicFitUsers", tenantId),
            new FakeTenantService());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Valid_tenant_token_sets_context_and_reaches_the_endpoint()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = new FakeTenantService(tenantId);
        var (context, nextCalled) = await InvokeAsync(
            "/api/clients",
            Principal("LogicFitUsers", tenantId),
            tenantService);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(nextCalled);
        Assert.Equal(tenantId, tenantService.CurrentTenantId);
    }

    [Fact]
    public async Task Platform_route_accepts_platform_token_without_tenant_context()
    {
        var tenantService = new FakeTenantService();
        var (context, nextCalled) = await InvokeAsync(
            "/api/platform/dashboard",
            Principal("LogicFitPlatform"),
            tenantService);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(nextCalled);
        Assert.Null(tenantService.CurrentTenantId);
    }

    private static ClaimsPrincipal Principal(string audience, Guid? tenantId = null)
    {
        var claims = new List<Claim> { new("aud", audience) };
        if (tenantId.HasValue)
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static async Task<(HttpContext Context, bool NextCalled)> InvokeAsync(
        string path,
        ClaimsPrincipal principal,
        FakeTenantService tenantService,
        string? tenantHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = principal;
        if (tenantHeader is not null)
            context.Request.Headers["X-Tenant-Id"] = tenantHeader;

        var nextCalled = false;
        var middleware = new TenantMiddleware(_ =>
        {
            nextCalled = true;
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, tenantService);
        return (context, nextCalled);
    }

    private sealed class FakeTenantService : ITenantService
    {
        private readonly HashSet<Guid> _existingTenants;

        public FakeTenantService(params Guid[] existingTenants)
            => _existingTenants = existingTenants.ToHashSet();

        public Guid? CurrentTenantId { get; private set; }

        public Task SetTenantAsync(Guid tenantId)
        {
            CurrentTenantId = tenantId;
            return Task.CompletedTask;
        }

        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;

        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);

        public Task<bool> TenantExistsAsync(Guid tenantId)
            => Task.FromResult(_existingTenants.Contains(tenantId));

        public Task<Guid?> ResolveTenantIdAsync(string identifier)
            => Task.FromResult<Guid?>(null);
    }
}
