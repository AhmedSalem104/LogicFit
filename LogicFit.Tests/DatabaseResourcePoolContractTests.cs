using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class DatabaseResourcePoolContractTests
{
    [Fact]
    public void Resource_statuses_preserve_the_operator_pool_lifecycle()
    {
        Assert.Equal(8, Enum.GetValues<DatabaseResourceStatus>().Length);
        Assert.Contains(DatabaseResourceStatus.Available, Enum.GetValues<DatabaseResourceStatus>());
        Assert.Contains(DatabaseResourceStatus.RestorePending, Enum.GetValues<DatabaseResourceStatus>());
        Assert.Contains(DatabaseResourceStatus.Retired, Enum.GetValues<DatabaseResourceStatus>());
    }

    [Fact]
    public async Task Manual_provider_fails_closed_when_pool_has_no_capacity()
    {
        var provider = new ManualMonsterProvisioningProvider(new StubResourcePool(null));
        var tenantId = Guid.NewGuid();

        var result = await provider.ProvisionAsync(tenantId);

        Assert.Equal("AwaitingDatabaseCapacity", result.Status);
        Assert.Equal("DATABASE_CAPACITY_UNAVAILABLE", result.ErrorCode);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Null(result.DatabaseName);
    }

    private sealed class StubResourcePool(DatabaseResourceReservation? reservation) : IDatabaseResourcePool
    {
        public Task<DatabaseResourceReservation?> ReserveAvailableAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(reservation);

        public Task<bool> ReleaseAsync(Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
