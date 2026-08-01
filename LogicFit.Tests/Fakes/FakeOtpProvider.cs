using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Tests.Fakes;

internal sealed class FakeOtpProvider : IOtpSender
{
    public List<(string Phone, string Code, OtpPurpose Purpose)> Messages { get; } = new();
    public bool Fail { get; set; }

    public Task<OtpSendResult> SendAsync(
        string normalizedPhoneNumber,
        string code,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        if (Fail) throw new HttpRequestException("Simulated provider failure.");
        Messages.Add((normalizedPhoneNumber, code, purpose));
        return Task.FromResult(new OtpSendResult("Fake", $"fake-{Messages.Count}", OtpDeliveryStatus.Sent));
    }
}
