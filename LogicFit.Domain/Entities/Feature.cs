using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>A gate-able feature (see FeatureCodes). Global reference data.</summary>
public class Feature : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
