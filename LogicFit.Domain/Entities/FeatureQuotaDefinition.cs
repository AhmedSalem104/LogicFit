using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class FeatureQuotaDefinition : BaseEntity
{
    public Guid FeatureId { get; set; }
    public string ResourceKey { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int? DefaultLimit { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Feature Feature { get; set; } = null!;
}
