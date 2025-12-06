using LogicFit.Application.Common.Interfaces;

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

        // Try Header first (X-Tenant-Id)
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
        // Try Subdomain
        else if (context.Request.Host.Host.Contains('.'))
        {
            var subdomain = context.Request.Host.Host.Split('.')[0];
            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
            {
                await tenantService.SetTenantBySubdomainAsync(subdomain);
            }
        }
        // Try from JWT claims
        else
        {
            var tenantClaim = context.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
            {
                await tenantService.SetTenantAsync(tenantId);
            }
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenant(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
