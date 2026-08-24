using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>
/// Resolves the persisted workspace type for the tenant in the signed token and
/// evaluates the requested capability. This intentionally does not trust a
/// workspace type supplied by the browser or a request header.
/// </summary>
public sealed class WorkspaceCapabilityAuthorizationHandler : AuthorizationHandler<WorkspaceCapabilityRequirement>
{
    private readonly IApplicationDbContext _context;

    public WorkspaceCapabilityAuthorizationHandler(IApplicationDbContext context) => _context = context;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspaceCapabilityRequirement requirement)
    {
        var tenantClaim = context.User.FindFirstValue("TenantId");
        if (!Guid.TryParse(tenantClaim, out var tenantId)) return;

        var workspaceType = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => (WorkspaceType?)x.WorkspaceType)
            .SingleOrDefaultAsync();

        if (workspaceType.HasValue && WorkspaceCapabilities.IsAvailable(requirement.Capability, workspaceType.Value))
            context.Succeed(requirement);
    }
}
