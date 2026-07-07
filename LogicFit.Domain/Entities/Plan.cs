using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A SaaS plan offered by the platform to gyms. Platform-global (not tenant-scoped).
/// Nullable Max* limits mean "unlimited".
/// </summary>
public class Plan : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public int DurationInDays { get; set; } = 30;

    public int? MaxMembers { get; set; }
    public int? MaxCoaches { get; set; }
    public int? MaxBranches { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxStorageMB { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
    public virtual ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
}
