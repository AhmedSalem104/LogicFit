using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Restricted, short-lived opaque session used only to follow a pending application.</summary>
public class ApplicationTrackingSession : BaseEntity
{
    public Guid ApplicationRequestId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public ApplicationRequest ApplicationRequest { get; set; } = null!;
}
