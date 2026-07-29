using LogicFit.Application.Common.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace LogicFit.API.Middleware;

/// <summary>Applies the workspace subscription policy to every authenticated tenant request.</summary>
public class TenantAccessMiddleware
{
    private readonly RequestDelegate _next;

    public TenantAccessMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ITenantAccessGuard guard)
    {
        var isAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        if (isAnonymous
            || context.User.Identity?.IsAuthenticated != true
            || tenantService.CurrentTenantId is not { } tenantId)
        {
            await _next(context);
            return;
        }

        var decision = TenantAccessPolicy.Evaluate(await guard.GetStateAsync(tenantId, context.RequestAborted));
        if (decision.Block is { } block)
            throw new TenantAccessException(block.Code, block.HttpStatus);

        var endpoint = context.GetEndpoint();
        var allowsBilling = endpoint?.Metadata.GetMetadata<AllowWhenPendingApprovalAttribute>() is not null;
        var allowsReadOnlyMutation = endpoint?.Metadata.GetMetadata<AllowWhenWorkspaceReadOnlyAttribute>() is not null;
        var isSafeRead = HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method);

        if (decision.Mode == TenantAccessMode.BillingOnly && !allowsBilling)
            throw new TenantAccessException("TENANT_BILLING_ONLY", StatusCodes.Status403Forbidden);

        if (decision.Mode == TenantAccessMode.ReadOnly && !isSafeRead && !allowsReadOnlyMutation)
            throw new TenantAccessException("TENANT_READ_ONLY", StatusCodes.Status403Forbidden);

        await _next(context);
    }
}

public static class TenantAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantAccessGate(this IApplicationBuilder builder)
        => builder.UseMiddleware<TenantAccessMiddleware>();
}
