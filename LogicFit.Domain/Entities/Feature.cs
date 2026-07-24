using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>A gate-able feature (see FeatureCodes). Global reference data.</summary>
public class Feature : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; } = true;
    public FeatureLifecycleStatus Status { get; set; } = FeatureLifecycleStatus.Active;
    public bool SupportsQuota { get; set; }

    // Navigation
    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
