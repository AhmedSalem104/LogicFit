namespace LogicFit.Application.Common.Services;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";
    public string Provider { get; set; } = string.Empty;
    public string? DevelopmentFixedCode { get; set; }
    public bool AllowTemporaryFixedCode { get; set; }
    public string? TemporaryFixedCode { get; set; }
    public DateTime? TemporaryFixedCodeExpiresAtUtc { get; set; }
    public string HmacSecret { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int DailySendLimit { get; set; } = 10;
    public bool RequireForInviteAcceptance { get; set; }
}
