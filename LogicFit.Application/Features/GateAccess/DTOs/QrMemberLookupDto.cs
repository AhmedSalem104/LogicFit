namespace LogicFit.Application.Features.GateAccess.DTOs;

public sealed class QrMemberLookupDto
{
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public Guid MembershipCardId { get; init; }
    public string CardNumber { get; init; } = string.Empty;
    public bool CardActive { get; init; }
    public DateTime? CardExpiresAt { get; init; }
    public bool SubscriptionActive { get; init; }
    public string? SubscriptionStatus { get; init; }
    public string? PlanName { get; init; }
    public DateTime? SubscriptionStartDate { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public decimal? RemainingAmount { get; init; }
}
