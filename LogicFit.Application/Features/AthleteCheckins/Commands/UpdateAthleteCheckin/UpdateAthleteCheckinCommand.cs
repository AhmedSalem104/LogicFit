using MediatR;

namespace LogicFit.Application.Features.AthleteCheckins.Commands.UpdateAthleteCheckin;

public sealed class UpdateAthleteCheckinCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public double? SleepHours { get; set; }
    public int? SleepQuality { get; set; }
    public int? Fatigue { get; set; }
    public int? Soreness { get; set; }
    public int? Stress { get; set; }
    public int? Mood { get; set; }
    public int? RestingHeartRate { get; set; }
    public double? Hrv { get; set; }
    public double? BodyweightKg { get; set; }
    public string? Notes { get; set; }
}
