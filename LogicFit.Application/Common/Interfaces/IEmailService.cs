namespace LogicFit.Application.Common.Interfaces;

/// <summary>Email delivery channel. The default implementation logs; wire SMTP/provider in production.</summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
