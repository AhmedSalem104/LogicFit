using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public sealed class OtpStepUpSession : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public Guid OtpChallengeId { get; set; }
    public OtpPurpose Purpose { get; set; } = OtpPurpose.SensitiveActionStepUp;
    public string TokenHash { get; set; } = string.Empty;
    public string? SessionBinding { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
