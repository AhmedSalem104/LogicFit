using MediatR;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.CreateBodyMeasurement;

public class CreateBodyMeasurementCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public DateTime DateRecorded { get; set; }
    public double WeightKg { get; set; }
    public double? SkeletalMuscleMass { get; set; }
    public double? BodyFatMass { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? TotalBodyWater { get; set; }
    public double? Bmr { get; set; }
    public int? VisceralFatLevel { get; set; }
    public IFormFile? InbodyImage { get; set; }
    public IFormFile? FrontPhoto { get; set; }
    public IFormFile? SidePhoto { get; set; }
    public IFormFile? BackPhoto { get; set; }
}
