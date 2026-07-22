using LogicFit.Application.Common.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>
/// Succeeds unless the current gym is <c>PendingApproval</c> and the endpoint is NOT marked
/// <see cref="AllowWhenPendingApprovalAttribute"/>. Platform users (no current tenant) always pass —
/// they are governed by permission requirements, not tenant status.
/// </summary>
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
        // Platform users / requests without a resolved tenant are not subject to gym-status rules.
        if (_tenantService.CurrentTenantId is not { } tenantId)
        {
            context.Succeed(requirement);
            return;
        }

        var state = await _tenantAccessGuard.GetStateAsync(tenantId);

        if (!TenantAccessPolicy.IsPendingApproval(state))
        {
            // Active/Trial/PastDue → full access. Hard-blocked gyms are stopped by the middleware before here.
            context.Succeed(requirement);
            return;
        }

        // PendingApproval: allow only billing/onboarding endpoints.
        var endpoint = (context.Resource as HttpContext)?.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowWhenPendingApprovalAttribute>() is not null)
        {
            context.Succeed(requirement);
        }
        // Otherwise: leave unsatisfied → the result handler emits TENANT_PENDING_APPROVAL (403).
    }
}
