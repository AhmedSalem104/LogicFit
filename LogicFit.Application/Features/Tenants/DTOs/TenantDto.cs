using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Tenants.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
