namespace LogicFit.Infrastructure.Services;

public sealed class MetaWhatsAppOptions
{
    public const string SectionName = "MetaWhatsApp";
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateLanguage { get; set; } = "en_US";
    public string GraphApiVersion { get; set; } = "v21.0";
    public string? WebhookVerifyToken { get; set; }
    public string? AppSecret { get; set; }
}
