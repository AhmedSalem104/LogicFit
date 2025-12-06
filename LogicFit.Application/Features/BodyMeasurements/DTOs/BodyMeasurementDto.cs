namespace LogicFit.Application.Features.BodyMeasurements.DTOs;

public class BodyMeasurementDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime DateRecorded { get; set; }
    public double? WeightKg { get; set; }
    public double? SkeletalMuscleMass { get; set; }
    public double? BodyFatMass { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? TotalBodyWater { get; set; }
    public double? Bmr { get; set; }
    public int? VisceralFatLevel { get; set; }
    public string? InbodyImageUrl { get; set; }
    public string? FrontPhotoUrl { get; set; }
    public string? SidePhotoUrl { get; set; }
    public string? BackPhotoUrl { get; set; }
}

public class CreateBodyMeasurementDto
{
    public Guid ClientId { get; set; }
    public DateTime DateRecorded { get; set; }
    public double? WeightKg { get; set; }
    public double? SkeletalMuscleMass { get; set; }
    public double? BodyFatMass { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? TotalBodyWater { get; set; }
    public double? Bmr { get; set; }
    public int? VisceralFatLevel { get; set; }
}

public class UpdateBodyMeasurementDto
{
    public double? WeightKg { get; set; }
    public double? SkeletalMuscleMass { get; set; }
    public double? BodyFatMass { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? TotalBodyWater { get; set; }
    public double? Bmr { get; set; }
    public int? VisceralFatLevel { get; set; }
    public string? InbodyImageUrl { get; set; }
    public string? FrontPhotoUrl { get; set; }
    public string? SidePhotoUrl { get; set; }
    public string? BackPhotoUrl { get; set; }
}
