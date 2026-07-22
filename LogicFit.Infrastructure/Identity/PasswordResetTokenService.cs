using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LogicFit.Infrastructure.Identity;

public sealed class PasswordResetTokenService : IPasswordResetTokenService
{
    private readonly byte[] _secret;

    public PasswordResetTokenService(IConfiguration configuration, IHostEnvironment environment)
    {
        var secret = configuration["PasswordReset:Secret"] ?? configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Password reset secret is not configured.");

        _secret = Encoding.UTF8.GetBytes(secret);
        CanExposeToken = environment.IsDevelopment()
            && configuration.GetValue("PasswordReset:ExposeTokenInDevelopment", true);
    }

    public bool CanExposeToken { get; }

    public string GenerateToken() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public string HashToken(string token)
    {
        using var hmac = new HMACSHA256(_secret);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }

    public bool VerifyToken(string token, string storedHash)
    {
        try
        {
            var expected = Convert.FromBase64String(storedHash);
            var actual = Convert.FromBase64String(HashToken(token));
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            // Legacy plaintext reset codes are intentionally invalidated by this hardening change.
            return false;
        }
    }
}
