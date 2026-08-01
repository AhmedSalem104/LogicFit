using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Temporary pre-provider delivery adapter. It never sends a message and relies on the explicitly
/// disclosed test code in the hosted test UI. Challenge creation, hashing, expiry, attempts,
/// session binding, rate limits, audit, and atomic consumption remain enforced by OtpService.
/// </summary>
public sealed class TemporaryFixedOtpProvider : IOtpSender
{
    public Task<OtpSendResult> SendAsync(string normalizedPhoneNumber, string code, OtpPurpose purpose,
        CancellationToken cancellationToken)
        => Task.FromResult(new OtpSendResult(
            "TemporaryFixed",
            $"temporary-fixed-{Guid.NewGuid():N}",
            OtpDeliveryStatus.Sent));
}
