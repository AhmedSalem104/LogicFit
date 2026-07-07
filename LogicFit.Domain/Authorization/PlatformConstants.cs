namespace LogicFit.Domain.Authorization;

/// <summary>Well-known identifiers for the platform (SaaS owner) layer.</summary>
public static class PlatformConstants
{
    /// <summary>
    /// Sentinel tenant that owns platform users (PlatformOwner/PlatformAdmin). It satisfies the
    /// User→Tenant foreign key without introducing a nullable TenantId. Platform users still get
    /// cross-tenant visibility because their JWT carries no TenantId claim (CurrentTenantId stays
    /// null, bypassing tenant query filters). This tenant is excluded from all tenant listings.
    /// </summary>
    public static readonly Guid PlatformTenantId = new("00000000-0000-0000-0000-0000000000A1");
}
