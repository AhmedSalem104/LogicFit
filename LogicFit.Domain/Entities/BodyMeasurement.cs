using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class BodyMeasurement : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public DateTime DateRecorded { get; set; }

    // InBody Data
    public double WeightKg { get; set; }
    public double? SkeletalMuscleMass { get; set; }
    public double? BodyFatMass { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? TotalBodyWater { get; set; }
    public double? Bmr { get; set; }
    public int? VisceralFatLevel { get; set; }

    // Progress Photos
    public string? InbodyImageUrl { get; set; }
    public string? FrontPhotoUrl { get; set; }
    public string? SidePhotoUrl { get; set; }
    public string? BackPhotoUrl { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
}
