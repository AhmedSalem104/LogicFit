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

    // Navigation
    public virtual Feature Feature { get; set; } = null!;
}
