using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Platform.Tenants.DTOs;

public class PlatformTenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public TenantStatus Status { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int MembersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
