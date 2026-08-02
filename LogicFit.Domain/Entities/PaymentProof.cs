using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Immutable metadata for one private payment-proof upload. The file itself lives in private
/// storage; this row is the auditable, versioned contract used by Platform review.
/// </summary>
public sealed class PaymentProof : BaseEntity
{
    public Guid PaymentRequestId { get; set; }
    public int Version { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public bool IsCurrent { get; set; } = true;
    public string? UploadedBy { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public PaymentRequest PaymentRequest { get; set; } = null!;
}
