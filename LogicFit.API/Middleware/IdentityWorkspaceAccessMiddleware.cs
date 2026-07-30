using System.Security.Claims;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace LogicFit.API.Middleware;

/// <summary>
/// Enforces the shared identity -> membership -> local-account boundary for every authenticated
/// tenant request before workspace subscription and permission checks are evaluated.
/// </summary>
public sealed class IdentityWorkspaceAccessMiddleware
{
    private readonly RequestDelegate _next;

    public IdentityWorkspaceAccessMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantService tenantService,
        IIdentityWorkspaceAccessGuard accessGuard)
    {
        var isAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        if (isAnonymous ||
            context.User.Identity?.IsAuthenticated != true ||
            tenantService.CurrentTenantId is not { } workspaceId ||
            workspaceId == PlatformConstants.PlatformTenantId)
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new TenantAccessException("WORKSPACE_ACCOUNT_NOT_FOUND", StatusCodes.Status403Forbidden);

        var decision = await accessGuard.EvaluateAsync(userId, workspaceId, context.RequestAborted);
        if (decision.Mode == IdentityWorkspaceAccessMode.Blocked)
            throw new TenantAccessException(decision.Code ?? "WORKSPACE_ACCESS_DENIED", StatusCodes.Status403Forbidden);

        await _next(context);
    }
}

public static class IdentityWorkspaceAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseIdentityWorkspaceAccessGate(this IApplicationBuilder builder) =>
        builder.UseMiddleware<IdentityWorkspaceAccessMiddleware>();
}
