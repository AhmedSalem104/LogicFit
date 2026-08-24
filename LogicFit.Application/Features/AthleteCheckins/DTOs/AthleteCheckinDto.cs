namespace LogicFit.Application.Features.AthleteCheckins.DTOs;

public sealed class AthleteCheckinDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime CheckinDate { get; set; }
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

    /// <summary>A simple coaching readiness indicator, not a medical score.</summary>
    public int? ReadinessScore
    {
        get
        {
            var values = new[]
            {
                SleepQuality,
                Fatigue.HasValue ? 6 - Fatigue.Value : null,
                Soreness.HasValue ? 6 - Soreness.Value : null,
                Stress.HasValue ? 6 - Stress.Value : null,
                Mood
            }.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return values.Count == 0 ? null : (int)Math.Round(values.Average() * 20, MidpointRounding.AwayFromZero);
        }
    }
}
