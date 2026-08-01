namespace LogicFit.Infrastructure.Services;

/// <summary>Public frontend URL is supplied by server configuration, never source control.</summary>
public sealed class IdentityEmailLinkOptions
{
    public const string SectionName = "IdentityEmailLinks";

    public string FrontendBaseUrl { get; init; } = string.Empty;
}
