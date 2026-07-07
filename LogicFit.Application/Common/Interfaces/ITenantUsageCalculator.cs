namespace LogicFit.Application.Common.Interfaces;

public record TenantUsageSnapshot(int Members, int Coaches, int Branches, int Employees);

/// <summary>Computes a tenant's live resource usage (used by the dashboard and the usage endpoints).</summary>
public interface ITenantUsageCalculator
{
    Task<TenantUsageSnapshot> CalculateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
