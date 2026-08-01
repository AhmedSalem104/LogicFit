using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public interface IOtpSender
{
    Task<OtpSendResult> SendAsync(
        string normalizedPhoneNumber,
        string code,
        OtpPurpose purpose,
        CancellationToken cancellationToken);
}

public sealed record OtpSendResult(string Provider, string? ProviderMessageId, OtpDeliveryStatus Status);
