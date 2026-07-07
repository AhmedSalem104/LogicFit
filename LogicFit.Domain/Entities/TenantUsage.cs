using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Denormalized per-tenant usage counters, refreshed by the background job. Advisory (for the
/// platform dashboard); quota enforcement uses live counts, not this cache. One row per tenant.
/// </summary>
public class TenantUsage : BaseEntity
{
    public Guid TenantId { get; set; }
    public int MembersCount { get; set; }
    public int CoachesCount { get; set; }
    public int EmployeesCount { get; set; }
    public int BranchesCount { get; set; }
    public int StorageUsedMB { get; set; }
    public DateTime LastCalculatedAt { get; set; }
}
