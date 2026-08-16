using MediatR;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.UpdateBodyMeasurement;

public sealed class UpdateBodyMeasurementCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public double? WeightKg { get; set; }
    public double? HeightCm { get; set; }
    public double? ChestCm { get; set; }
    public double? WaistCm { get; set; }
    public double? HipsCm { get; set; }
    public double? ArmsCm { get; set; }
    public double? ThighsCm { get; set; }
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
    public string? Notes { get; set; }
}
