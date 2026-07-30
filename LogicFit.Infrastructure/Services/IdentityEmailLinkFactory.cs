using LogicFit.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Places the opaque token in the URL fragment so it is not sent in the HTTP request for the
/// frontend document. The frontend reads it and posts it over HTTPS to the API.
/// </summary>
public sealed class IdentityEmailLinkFactory : IIdentityEmailLinkFactory
{
    private readonly IdentityEmailLinkOptions _options;

    public IdentityEmailLinkFactory(IOptions<IdentityEmailLinkOptions> options) => _options = options.Value;

    public bool IsConfigured => Uri.TryCreate(_options.FrontendBaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public string CreateEmailVerificationLink(string rawToken) => Create("identity/verify-email", rawToken);

    public string CreatePasswordResetLink(string rawToken) => Create("identity/reset-password", rawToken);

    private string Create(string route, string rawToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Identity email links are not configured.");

        return $"{_options.FrontendBaseUrl.TrimEnd('/')}/{route}#token={Uri.EscapeDataString(rawToken)}";
    }
}
