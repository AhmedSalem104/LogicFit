namespace LogicFit.Application.Common.Interfaces;

public interface ITenantService
{
    Guid? CurrentTenantId { get; }
    Guid GetCurrentTenantId() => CurrentTenantId ?? throw new InvalidOperationException("Tenant not set");
    Task SetTenantAsync(Guid tenantId);
    Task SetTenantBySubdomainAsync(string subdomain);
    Task<bool> TenantExistsAsync(Guid tenantId);
}
