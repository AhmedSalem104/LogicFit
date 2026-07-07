using MediatR;

namespace LogicFit.Application.Features.Branding;

/// <summary>Public lookup of a gym's white-label branding by subdomain or custom domain (for theming pre-login).</summary>
public class GetTenantBrandingQuery : IRequest<BrandingDto?>
{
    public string Identifier { get; set; } = string.Empty;
}

public class BrandingDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? AppName { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? FontFamily { get; set; }
    public string? CustomCss { get; set; }
    public string? InvoiceLogoUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}
