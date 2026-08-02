using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Identity.DTOs;

public sealed class OtpChallengeDto
{
    public Guid ChallengeId { get; init; }
    public OtpPurpose Purpose { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime ResendAvailableAtUtc { get; init; }
    public string MaskedPhoneNumber { get; init; } = string.Empty;
}
