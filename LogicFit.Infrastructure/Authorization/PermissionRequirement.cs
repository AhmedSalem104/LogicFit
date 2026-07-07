using Microsoft.AspNetCore.Authorization;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>An authorization requirement for a single permission code.</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
