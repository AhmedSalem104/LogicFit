using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.ValueObjects;

namespace LogicFit.Domain.Entities;

public class Tenant : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BrandingSettings? BrandingSettings { get; set; }

    // Gym Details
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? GalleryImagesJson { get; set; } // JSON array of image URLs

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Food> Foods { get; set; } = new List<Food>();
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public virtual ICollection<DietPlan> DietPlans { get; set; } = new List<DietPlan>();
    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
}
