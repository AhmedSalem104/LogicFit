using LogicFit.Domain.Authorization;
using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>
/// Succeeds when the current principal carries a matching <c>permission</c> claim.
/// Holders of <see cref="Permissions.ManagePlatform"/> get god-mode (all permissions).
/// Pure and synchronous — permissions are embedded in the JWT at login.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";
    private readonly IApplicationDbContext _context;

    public PermissionAuthorizationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        var versionClaim = context.User.FindFirstValue("perm_ver");

        if (!Guid.TryParse(userIdValue, out var userId) || !int.TryParse(versionClaim, out var tokenVersion))
            return;

        var currentVersion = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => (int?)u.PermissionsVersion)
            .FirstOrDefaultAsync();

        if (!currentVersion.HasValue || currentVersion.Value != tokenVersion)
            return;

        var permissions = context.User.FindAll(PermissionClaimType);

        foreach (var claim in permissions)
        {
            if (claim.Value == requirement.Permission ||
                claim.Value == Permissions.ManagePlatform)
            {
                context.Succeed(requirement);
                break;
            }
        }
    }
}
