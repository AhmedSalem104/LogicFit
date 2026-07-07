using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>
/// Succeeds when the current principal carries a matching <c>permission</c> claim.
/// Holders of <see cref="Permissions.ManagePlatform"/> get god-mode (all permissions).
/// Pure and synchronous — permissions are embedded in the JWT at login.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
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

        return Task.CompletedTask;
    }
}
