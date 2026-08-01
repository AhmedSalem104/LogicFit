using System.Security.Cryptography;
using System.Text;

namespace LogicFit.Application.Features.Identity;

/// <summary>Creates opaque 256-bit link tokens and their database-safe SHA-256 representation.</summary>
public static class IdentityEmailActionToken
{
    private const int TokenSizeBytes = 32;

    public static string CreateRaw()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Hash(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ArgumentException("An email action token is required.", nameof(rawToken));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
