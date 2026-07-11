namespace LogicFit.Application.Common.Interfaces;

public interface ITenantService
{
    Guid? CurrentTenantId { get; }
    Guid GetCurrentTenantId() => CurrentTenantId ?? throw new InvalidOperationException("Tenant not set");
    Task SetTenantAsync(Guid tenantId);
    Task SetTenantBySubdomainAsync(string subdomain);
    Task<bool> SetTenantByCustomDomainAsync(string host);
    Task<bool> TenantExistsAsync(Guid tenantId);

    /// <summary>Resolves a tenant id from its subdomain or custom domain (case-insensitive). Null if not found.</summary>
    Task<Guid?> ResolveTenantIdAsync(string identifier);
}
