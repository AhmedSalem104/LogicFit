using LogicFit.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Default email channel: logs the message. Replace with an SMTP/provider implementation in
/// production (register a different IEmailService).
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject} | {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
