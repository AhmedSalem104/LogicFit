using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class TaxSetting : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
