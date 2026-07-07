namespace LogicFit.Domain.ValueObjects;

public class BrandingSettings
{
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? FontFamily { get; set; }
    public string? CustomCss { get; set; }

    // White-label
    public string? AppName { get; set; }
    public string? InvoiceLogoUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}
