using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

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

        var isAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
        var isPlatformRoute = IsPlatformRoute(path);

        // Platform routes are intentionally tenantless, but a tenant token must not be
        // accepted on them. Anonymous platform login/refresh continues to work.
        if (isPlatformRoute)
        {
            if (isAuthenticated && !HasAudience(context.User!, "LogicFitPlatform"))
            {
                await RejectAsync(context, StatusCodes.Status403Forbidden, "A platform token is required.");
                return;
            }

            await _next(context);
            return;
        }

        // A platform token is never a tenant token. Reject it on every non-platform route,
        // including endpoints marked AllowAnonymous, unless a future endpoint is deliberately
        // moved under /api/platform or receives an explicit reviewed exception.
        if (isAuthenticated && HasAudience(context.User!, "LogicFitPlatform"))
        {
            await RejectAsync(context, StatusCodes.Status403Forbidden, "A tenant token is required.");
            return;
        }

        // An authenticated request to a non-platform API is a tenant request. It must use a
        // tenant-audience token with a signed TenantId claim. Header/host resolution is only
        // retained for anonymous public flows; accepting X-Tenant-Id for an authenticated token
        // would let a caller try to switch the query-filter context independently of the token.
        if (isAuthenticated && !isAnonymous)
        {
            if (!HasAudience(context.User!, "LogicFitUsers"))
            {
                await RejectAsync(context, StatusCodes.Status403Forbidden, "A tenant audience is required.");
                return;
            }

            var tenantClaim = context.User!.FindFirst("TenantId")?.Value;
            if (!Guid.TryParse(tenantClaim, out var tenantIdFromClaim) || tenantIdFromClaim == Guid.Empty)
            {
                await RejectAsync(context, StatusCodes.Status403Forbidden, "Tenant context is required.");
                return;
            }

            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
            {
                if (!Guid.TryParse(tenantIdHeader.ToString(), out var headerTenantId) || headerTenantId != tenantIdFromClaim)
                {
                    await RejectAsync(context, StatusCodes.Status403Forbidden, "Tenant context does not match the token.");
                    return;
                }
            }

            if (!await tenantService.TenantExistsAsync(tenantIdFromClaim))
            {
                await RejectAsync(context, StatusCodes.Status403Forbidden, "Tenant context is invalid.");
                return;
            }

            await tenantService.SetTenantAsync(tenantIdFromClaim);
            await _next(context);
            return;
        }

        // Anonymous public flows may resolve a tenant from an explicit header or host. They
        // never receive an authenticated tenant context, and protected endpoints still stop at
        // ASP.NET Core authorization.
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var anonymousTenantHeader))
        {
            if (Guid.TryParse(anonymousTenantHeader.ToString(), out var tenantId))
            {
                if (!await tenantService.TenantExistsAsync(tenantId))
                {
                    await RejectAsync(context, StatusCodes.Status400BadRequest, "Invalid tenant.");
                    return;
                }

                await tenantService.SetTenantAsync(tenantId);
            }
            else
            {
                await RejectAsync(context, StatusCodes.Status400BadRequest, "Invalid tenant.");
                return;
            }
        }
        else
        {
            // Try a custom domain (full host), then fall back to subdomain.
            var host = context.Request.Host.Host;
            var matchedCustomDomain = await tenantService.SetTenantByCustomDomainAsync(host);

            if (!matchedCustomDomain && host.Contains('.'))
            {
                var subdomain = host.Split('.')[0];
                if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
                    await tenantService.SetTenantBySubdomainAsync(subdomain);
            }
        }

        await _next(context);
    }

    private static bool HasAudience(ClaimsPrincipal user, string expectedAudience)
    {
        return user.FindAll(JwtRegisteredClaimNames.Aud)
            .Concat(user.FindAll("aud"))
            .Any(c => string.Equals(c.Value, expectedAudience, StringComparison.Ordinal));
    }

    private static bool IsPlatformRoute(string? path)
    {
        return string.Equals(path, "/api/platform", StringComparison.OrdinalIgnoreCase)
            || (path?.StartsWith("/api/platform/", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static async Task RejectAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenant(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
