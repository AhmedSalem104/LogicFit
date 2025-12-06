using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class MealLog : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid MealItemId { get; set; }
    public double ConsumedQuantity { get; set; }
    public DateTime ConsumedAt { get; set; }
    public int? AlternativeFoodId { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
    public virtual MealItem MealItem { get; set; } = null!;
    public virtual Food? AlternativeFood { get; set; }
}
