using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// One daily readiness check-in for a client.  It is tenant-scoped and intentionally
/// separate from gym attendance: a coaching check-in can exist for an external trainee.
/// </summary>
public class AthleteCheckin : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
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

    public virtual User Client { get; set; } = null!;
}
