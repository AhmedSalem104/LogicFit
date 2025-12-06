namespace LogicFit.Application.Features.GymProfile.DTOs;

public class GymProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> GalleryImages { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public BrandingSettingsDto? BrandingSettings { get; set; }
    public GymStatisticsDto Statistics { get; set; } = new();
}

public class BrandingSettingsDto
{
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LogoUrl { get; set; }
}

public class GymStatisticsDto
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int TotalCoaches { get; set; }
    public int TotalSubscriptionPlans { get; set; }
    public int ActiveSubscriptions { get; set; }
}
