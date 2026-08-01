namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Security-sensitive email delivery boundary. Callers may pass a verification or reset link,
/// but implementations must never log the message body or raw link/token.
/// </summary>
public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody, string TextBody);
