using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace LogicFit.API.Middleware;

/// <summary>
/// Per-request hard gate: rejects any protected request whose gym is not allowed to be served
/// (suspended / expired / cancelled / archived / deleted), even if the caller holds a token issued
/// before the gym was suspended. Deliberately simple — it only decides blocked vs. not-blocked; the
/// finer "PendingApproval → billing only" rule lives in the authorization layer.
/// </summary>
public class TenantAccessMiddleware
{
    private readonly RequestDelegate _next;

    public TenantAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ITenantAccessGuard guard)
    {
        // Skip anonymous endpoints (login/register/branding/refresh), unauthenticated requests, and
        // platform users (no resolved tenant). Health/swagger are anonymous + tenant-less, so covered.
        var isAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        if (isAnonymous
            || context.User.Identity?.IsAuthenticated != true
            || tenantService.CurrentTenantId is not { } tenantId)
        {
            await _next(context);
            return;
        }

        var state = await guard.GetStateAsync(tenantId, context.RequestAborted);
        if (TenantAccessPolicy.EvaluateHardBlock(state) is { } block)
        {
            throw new TenantAccessException(block.Code, block.HttpStatus);
        }

        await _next(context);
    }
}

public static class TenantAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantAccessGate(this IApplicationBuilder builder)
        => builder.UseMiddleware<TenantAccessMiddleware>();
}
