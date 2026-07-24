using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Per-tenant feature override (grant/revoke or re-limit beyond the plan). Platform-managed,
/// keyed by TenantId, not tenant query-filtered.
/// </summary>
public class TenantFeature : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid FeatureId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? LimitOverride { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public Guid? GrantedByUserId { get; set; }

    // Navigation
    public virtual Feature Feature { get; set; } = null!;
}
