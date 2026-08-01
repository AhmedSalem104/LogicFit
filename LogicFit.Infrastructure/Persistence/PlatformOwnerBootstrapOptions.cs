using System.Net.Mail;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Explicit, temporary operator configuration used to create or repair the first Platform Owner.
/// Values must come from the server secret store and the switch must be disabled after recovery.
/// </summary>
public sealed class PlatformOwnerBootstrapOptions
{
    public const string SectionName = "PlatformBootstrap";

    public bool Enabled { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string FullName { get; set; } = "Platform Owner";
    public bool ResetPassword { get; set; }

    public string GetNormalizedEmail() => IdentityEmailAddress.Normalize(Email!);
    public string GetNormalizedPhoneNumber() => OtpService.NormalizePhone(PhoneNumber!);

    public static void Validate(PlatformOwnerBootstrapOptions options)
    {
        if (!options.Enabled) return;

        if (string.IsNullOrWhiteSpace(options.Email))
            throw new InvalidOperationException("PlatformBootstrap:Email is required when bootstrap is enabled.");
        try
        {
            var parsed = new MailAddress(options.Email.Trim());
            if (!string.Equals(parsed.Address, options.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new FormatException();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("PlatformBootstrap:Email must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(options.PhoneNumber))
            throw new InvalidOperationException("PlatformBootstrap:PhoneNumber is required when bootstrap is enabled.");
        try
        {
            _ = options.GetNormalizedPhoneNumber();
        }
        catch
        {
            throw new InvalidOperationException("PlatformBootstrap:PhoneNumber must be a valid E.164 number.");
        }

        if (string.IsNullOrWhiteSpace(options.Password) ||
            options.Password.Length < 12 ||
            !options.Password.Any(char.IsUpper) ||
            !options.Password.Any(char.IsLower) ||
            !options.Password.Any(char.IsDigit) ||
            !options.Password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException(
                "PlatformBootstrap:Password must contain at least 12 characters, uppercase, lowercase, digit, and symbol.");
        }

        if (string.IsNullOrWhiteSpace(options.FullName) || options.FullName.Trim().Length > 200)
            throw new InvalidOperationException("PlatformBootstrap:FullName must contain 1 to 200 characters.");
    }
}
