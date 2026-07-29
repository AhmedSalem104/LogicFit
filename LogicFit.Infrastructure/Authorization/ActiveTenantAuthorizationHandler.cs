using LogicFit.Application.Common.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>Applies the access mode to permission-protected tenant endpoints.</summary>
public class ActiveTenantAuthorizationHandler : AuthorizationHandler<ActiveTenantRequirement>
{
    private readonly ITenantService _tenantService;
    private readonly ITenantAccessGuard _tenantAccessGuard;

    public ActiveTenantAuthorizationHandler(ITenantService tenantService, ITenantAccessGuard tenantAccessGuard)
    {
        _tenantService = tenantService;
        _tenantAccessGuard = tenantAccessGuard;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveTenantRequirement requirement)
    {
        if (_tenantService.CurrentTenantId is not { } tenantId)
        {
            context.Succeed(requirement);
            return;
        }

        var decision = TenantAccessPolicy.Evaluate(await _tenantAccessGuard.GetStateAsync(tenantId));
        if (decision.Mode == TenantAccessMode.Full)
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();
        var allowsBilling = endpoint?.Metadata.GetMetadata<AllowWhenPendingApprovalAttribute>() is not null;
        var allowsReadOnlyMutation = endpoint?.Metadata.GetMetadata<AllowWhenWorkspaceReadOnlyAttribute>() is not null;
        var isSafeRead = httpContext is not null &&
            (HttpMethods.IsGet(httpContext.Request.Method)
             || HttpMethods.IsHead(httpContext.Request.Method)
             || HttpMethods.IsOptions(httpContext.Request.Method));

        if (decision.Mode == TenantAccessMode.BillingOnly && allowsBilling)
        {
            context.Succeed(requirement);
            return;
        }

        if (decision.Mode == TenantAccessMode.ReadOnly && (isSafeRead || allowsReadOnlyMutation))
            context.Succeed(requirement);
    }
}
