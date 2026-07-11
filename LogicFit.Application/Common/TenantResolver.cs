using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;

namespace LogicFit.Application.Common;

/// <summary>
/// Resolves the target gym for anonymous auth flows without forcing the client to pass a TenantId GUID.
/// Order: explicit tenantId → subdomain/custom-domain field → tenant resolved by middleware
/// (X-Tenant-Id header or host). Throws a clear validation error if none identify a gym.
/// </summary>
public static class TenantResolver
{
    public static async Task<Guid> ResolveAsync(Guid tenantId, string? subdomain, ITenantService tenantService)
    {
        if (tenantId != Guid.Empty)
        {
            return tenantId;
        }

        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            var resolved = await tenantService.ResolveTenantIdAsync(subdomain);
            if (resolved.HasValue)
            {
                return resolved.Value;
            }

            throw new ValidationException("subdomain", $"No gym found for '{subdomain}'.");
        }

        if (tenantService.CurrentTenantId.HasValue)
        {
            return tenantService.CurrentTenantId.Value;
        }

        throw new ValidationException("subdomain", "Could not identify the gym. Provide 'subdomain' (or 'tenantId').");
    }
}
