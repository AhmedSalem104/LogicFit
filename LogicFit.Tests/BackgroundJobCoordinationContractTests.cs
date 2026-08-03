using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class BackgroundJobCoordinationContractTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Outbox_idempotency_is_a_unique_bounded_database_key()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);
        var entity = context.Model.FindEntityType(typeof(OutboxMessage));

        Assert.NotNull(entity);
        Assert.Equal(200, entity.FindProperty(nameof(OutboxMessage.IdempotencyKey))?.GetMaxLength());

        var idempotencyIndex = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(OutboxMessage.IdempotencyKey));
        Assert.True(idempotencyIndex.IsUnique);
    }

    [Fact]
    public void Sql_server_lock_provider_holds_a_session_owned_application_lock()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "SqlServerDistributedLockProvider.cs"));

        Assert.Contains("sys.sp_getapplock", source);
        Assert.Contains("@LockOwner = 'Session'", source);
        Assert.Contains("@LockTimeout = @lockTimeoutMilliseconds", source);
        Assert.Contains("sys.sp_releaseapplock", source);
    }

    [Fact]
    public void Idempotency_migration_requires_operator_review_for_existing_duplicates()
    {
        var migration = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Persistence",
            "Migrations",
            "20260803100030_HardenBackgroundJobCoordination.cs"));

        Assert.Contains("COUNT_BIG(*) > 1", migration);
        Assert.Contains("THROW 51000", migration);
    }

    [Fact]
    public void Singleton_jobs_acquire_distinct_distributed_locks_before_processing()
    {
        var tenantLifecycle = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "SubscriptionLifecycleService.cs"));
        var platformLifecycle = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "PlatformSubscriptionLifecycleService.cs"));
        var outbox = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "OutboxProcessorService.cs"));

        Assert.Contains("IDistributedLockProvider", tenantLifecycle);
        Assert.Contains("TryAcquireAsync(LockResource", tenantLifecycle);
        Assert.Contains("IDistributedLockProvider", platformLifecycle);
        Assert.Contains("TryAcquireAsync(LockResource", platformLifecycle);
        Assert.Contains("IDistributedLockProvider", outbox);
        Assert.Contains("TryAcquireAsync(LockResource", outbox);
        Assert.NotEqual(
            ExtractLockResource(tenantLifecycle),
            ExtractLockResource(platformLifecycle));
        Assert.NotEqual(
            ExtractLockResource(platformLifecycle),
            ExtractLockResource(outbox));
    }

    private static string ExtractLockResource(string source)
    {
        const string marker = "LockResource = \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += marker.Length;
        var end = source.IndexOf('"', start);
        Assert.True(end > start);
        return source[start..end];
    }
}
