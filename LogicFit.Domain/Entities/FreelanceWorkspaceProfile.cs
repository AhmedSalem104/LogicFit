namespace LogicFit.Domain.Entities;

/// <summary>Structured public/professional profile for a FreelanceCoach workspace.</summary>
public class FreelanceWorkspaceProfile
{
    public Guid TenantId { get; set; }
    public string? Bio { get; set; }
    public string? SpecialtiesJson { get; set; }
    public string? CertificationsJson { get; set; }
    public string? SocialLinksJson { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? BookingSettingsJson { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
