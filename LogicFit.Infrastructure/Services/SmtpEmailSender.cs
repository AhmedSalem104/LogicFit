using System.Net;
using System.Net.Mail;
using LogicFit.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Services;

/// <summary>SMTP implementation. Credentials and message contents are never written to logs.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options) => _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Email delivery is not configured.");

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));
        mail.To.Add(new MailAddress(message.ToEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
