namespace LogicFit.Application.Features.MembershipCards.DTOs;

public class MembershipCardDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
