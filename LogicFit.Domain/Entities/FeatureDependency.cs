using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class FeatureDependency : BaseEntity
{
    public Guid FeatureId { get; set; }
    public Guid DependsOnFeatureId { get; set; }

    public virtual Feature Feature { get; set; } = null!;
    public virtual Feature DependsOnFeature { get; set; } = null!;
}
