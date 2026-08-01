using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public sealed class OtpChallenge : AuditableEntity
{
    public Guid? IdentityAccountId { get; set; }
    public string NormalizedPhoneNumber { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string CodeSalt { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public int ResendCount { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public OtpChallengeStatus Status { get; set; } = OtpChallengeStatus.Pending;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public OtpDeliveryStatus DeliveryStatus { get; set; } = OtpDeliveryStatus.Queued;
    public DateTime CreatedAtUtc { get; set; }
    public string? SessionBinding { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IdentityAccount? IdentityAccount { get; set; }
}
