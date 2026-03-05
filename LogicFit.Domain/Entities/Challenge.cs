using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Challenge : TenantAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TargetMetric { get; set; } // e.g., "sessions", "weight_loss", "streak"
    public double? TargetValue { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;
    public Guid CreatedByCoachId { get; set; }

    // Navigation Properties
    public virtual User CreatedByCoach { get; set; } = null!;
    public virtual ICollection<ClientChallenge> Participants { get; set; } = new List<ClientChallenge>();
}
