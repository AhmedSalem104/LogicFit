using Microsoft.AspNetCore.Authorization;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement enforcing that the current gym is allowed to use the requested endpoint.
/// Hard blocks (suspended/expired) are handled earlier by the tenant-access middleware; this requirement
/// enforces the business rule that a <c>PendingApproval</c> gym may only reach endpoints marked
/// <c>[AllowWhenPendingApproval]</c> (billing / onboarding).
/// </summary>
public class ActiveTenantRequirement : IAuthorizationRequirement
{
}
