using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Notifications;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public sealed class OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(1);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Outbox processor failed"); }
            await Task.Delay(Period, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var messages = await context.OutboxMessages.Where(x => x.ProcessedAtUtc == null && x.AttemptCount < 5)
            .OrderBy(x => x.OccurredAtUtc).Take(20).ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                message.AttemptCount++;
                if (message.Type == "tenant.subscription.expired")
                {
                    using var json = JsonDocument.Parse(message.Payload);
                    var tenantId = json.RootElement.GetProperty("tenantId").GetGuid();
                    await notifier.NotifyTenantOwnerAsync(tenantId, NotificationTemplates.TenantSuspended, null, cancellationToken);
                }
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.FailedAtUtc = null;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.FailedAtUtc = DateTime.UtcNow;
                message.LastError = ex.Message;
                logger.LogError(ex, "Outbox message {MessageId} failed", message.Id);
            }
        }
        if (messages.Count > 0) await context.SaveChangesAsync(cancellationToken);
    }
}
