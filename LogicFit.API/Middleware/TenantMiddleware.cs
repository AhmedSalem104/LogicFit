using System.Security.Claims;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;

namespace LogicFit.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Skip tenant resolution for certain paths
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.Contains("/swagger") || path.Contains("/health")))
        {
            await _next(context);
            return;
        }

        // 1. First try from JWT claims (most reliable for authenticated requests)
        var tenantClaim = context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantIdFromClaim))
        {
            await tenantService.SetTenantAsync(tenantIdFromClaim);
            await _next(context);
            return;
        }

        // 2. Try Header (X-Tenant-Id) for unauthenticated requests or when JWT doesn't have TenantId
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                var tenantExists = await tenantService.TenantExistsAsync(tenantId);
                if (tenantExists)
                {
                    await tenantService.SetTenantAsync(tenantId);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant" });
                    return;
                }
            }
        }
        // 3. Try a custom domain (full host), then fall back to subdomain.
        else
        {
            var host = context.Request.Host.Host;
            var matchedCustomDomain = await tenantService.SetTenantByCustomDomainAsync(host);

            if (!matchedCustomDomain && host.Contains('.'))
            {
                var subdomain = host.Split('.')[0];
                if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
                {
                    await tenantService.SetTenantBySubdomainAsync(subdomain);
                }
            }
        }

        // Hardening: an authenticated, non-platform user must have a resolved tenant.
        // Otherwise CurrentTenantId stays null, which bypasses every tenant query filter
        // (that null-bypass is reserved for platform users), leaking cross-tenant data.
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
        if (isAuthenticated && tenantService.CurrentTenantId == null && !IsPlatformUser(context.User!))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant could not be resolved" });
            return;
        }

        await _next(context);
    }

    private static bool IsPlatformUser(ClaimsPrincipal user)
    {
        // Platform users carry platform roles / permissions and no TenantId claim.
        if (user.IsInRole(SystemRoles.PlatformOwner) || user.IsInRole(SystemRoles.PlatformAdmin))
        {
            return true;
        }

        return user.FindAll("permission")
            .Any(c => Permissions.PlatformPermissions.Contains(c.Value));
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenant(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
