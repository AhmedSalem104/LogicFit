using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public class SubscriptionLifecycleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionLifecycleService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public SubscriptionLifecycleService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionLifecycleService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionLifecycleService started");

        // Run immediately on startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubscriptionLifecycleService");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task ProcessSubscriptions(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ExpireSubscriptions(context, cancellationToken);
        await UnfreezeSubscriptions(context, cancellationToken);
    }

    private async Task ExpireSubscriptions(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredSubscriptions = await context.ClientSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate < now
                && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = SubscriptionStatus.Expired;
        }

        if (expiredSubscriptions.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expired {Count} subscriptions", expiredSubscriptions.Count);
        }
    }

    private async Task UnfreezeSubscriptions(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredFreezes = await context.SubscriptionFreezes
            .IgnoreQueryFilters()
            .Include(f => f.Subscription)
            .Where(f => f.IsActive && f.EndDate <= now && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var freeze in expiredFreezes)
        {
            freeze.IsActive = false;

            if (freeze.Subscription.Status == SubscriptionStatus.Suspended)
            {
                freeze.Subscription.Status = SubscriptionStatus.Active;
            }
        }

        if (expiredFreezes.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Unfroze {Count} subscriptions", expiredFreezes.Count);
        }
    }
}
