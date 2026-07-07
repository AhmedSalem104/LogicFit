using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Join between <see cref="Plan"/> and <see cref="Feature"/>, with an optional numeric limit.</summary>
public class PlanFeature : BaseEntity
{
    public Guid PlanId { get; set; }
    public Guid FeatureId { get; set; }
    public int? LimitValue { get; set; }

    // Navigation
    public virtual Plan Plan { get; set; } = null!;
    public virtual Feature Feature { get; set; } = null!;
}
