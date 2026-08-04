using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace LogicFit.API.Middleware;

/// <summary>
/// Resolves the workspace database after TenantMiddleware has established TenantId and before
/// any authorization/handler code can access a tenant-owned DbSet.
/// </summary>
public sealed class TenantDatabaseRoutingMiddleware(
    RequestDelegate next,
    IOptions<TenantDatabaseRoutingOptions> options,
    ILogger<TenantDatabaseRoutingMiddleware> logger)
{
    private readonly TenantDatabaseRoutingOptions _options = options.Value;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantService tenantService,
        ITenantDatabaseResolver resolver,
        TenantDatabaseRequestScope requestScope)
    {
        if (!_options.Enabled || !tenantService.CurrentTenantId.HasValue)
        {
            await next(context);
            return;
        }

        var tenantId = tenantService.CurrentTenantId.Value;
        TenantDatabaseResolution? resolution;
        try
        {
            resolution = await resolver.ResolveAsync(tenantId, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Tenant database resolution failed for TenantId {TenantId}; request was stopped.",
                tenantId);
            resolution = null;
        }

        if (resolution is null || resolution.TenantId != tenantId)
        {
            if (!_options.FailClosedWithoutMapping)
            {
                logger.LogWarning(
                    "Tenant database routing is enabled but no valid mapping exists for TenantId {TenantId}; fail-open was explicitly configured.",
                    tenantId);
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    errorCode = "TENANT_DATABASE_UNAVAILABLE",
                    message = "Workspace database is not ready. Contact the platform administrator."
                },
                context.RequestAborted);
            return;
        }

        requestScope.Set(resolution);
        try
        {
            await next(context);
        }
        finally
        {
            requestScope.Clear();
        }
    }
}

public static class TenantDatabaseRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantDatabaseRouting(this IApplicationBuilder builder)
        => builder.UseMiddleware<TenantDatabaseRoutingMiddleware>();
}
