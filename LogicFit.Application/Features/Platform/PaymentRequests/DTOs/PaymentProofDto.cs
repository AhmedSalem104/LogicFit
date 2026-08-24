namespace LogicFit.Application.Features.Platform.PaymentRequests.DTOs;

/// <summary>
/// Safe, non-secret metadata for one retained payment-proof version.
/// Storage keys are intentionally never returned to the client.
/// </summary>
public sealed class PaymentProofDto
{
    public Guid Id { get; init; }
    public int Version { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public string? UploadedBy { get; init; }
    public DateTime UploadedAtUtc { get; init; }
}
