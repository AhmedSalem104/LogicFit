using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Infrastructure.Services;

public sealed class DevelopmentOtpProvider : IOtpSender
{
    public Task<OtpSendResult> SendAsync(string normalizedPhoneNumber, string code, OtpPurpose purpose,
        CancellationToken cancellationToken)
        => Task.FromResult(new OtpSendResult("Development", $"dev-{Guid.NewGuid():N}", OtpDeliveryStatus.Sent));
}
