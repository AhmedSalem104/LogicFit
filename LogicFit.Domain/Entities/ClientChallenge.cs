using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ClientChallenge : TenantAuditableEntity
{
    public Guid ChallengeId { get; set; }
    public Guid ClientId { get; set; }
    public double CurrentProgress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public virtual Challenge Challenge { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
}
