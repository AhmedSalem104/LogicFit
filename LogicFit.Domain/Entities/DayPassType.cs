using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class DayPassType : TenantAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public virtual ICollection<DayPassSale> Sales { get; set; } = new List<DayPassSale>();
}
