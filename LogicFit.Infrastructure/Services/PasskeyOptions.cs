namespace LogicFit.Infrastructure.Services;

/// <summary>Relying-party configuration. Production values belong in server secrets, never source control.</summary>
public sealed class PasskeyOptions
{
    public const string SectionName = "Passkeys";
    public string ServerDomain { get; init; } = string.Empty;
    public string ServerName { get; init; } = "LogicFit";
    public string[] Origins { get; init; } = Array.Empty<string>();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerDomain) && Origins.All(x =>
        Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
}
