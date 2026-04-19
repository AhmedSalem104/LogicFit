using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class MembershipCard : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    public virtual User Client { get; set; } = null!;
}
