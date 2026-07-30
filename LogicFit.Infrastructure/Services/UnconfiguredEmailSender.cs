using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Safe default while production email secrets have not been configured. It deliberately does not
/// log recipient, subject, body, links, or tokens.
/// </summary>
public sealed class UnconfiguredEmailSender : IEmailSender
{
    public bool IsConfigured => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Email delivery is not configured.");
}
