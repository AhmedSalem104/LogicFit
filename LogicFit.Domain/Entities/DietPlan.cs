using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class DietPlan : TenantAuditableEntity
{
    public Guid CoachId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PlanStatus Status { get; set; }

    // Daily Targets
    public double TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFats { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User Coach { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
    public virtual ICollection<DailyMeal> Meals { get; set; } = new List<DailyMeal>();
}
