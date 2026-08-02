using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantDatabaseResolverTests
{
    [Fact]
    public async Task Resolves_only_an_assigned_mapping_reserved_for_the_same_tenant()
    {
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var reader = new StubMappingReader(new TenantDatabaseMappingRecord(
            Guid.NewGuid(),
            tenantId,
            resourceId,
            "ManualMonster",
            "monster-db-17",
            DatabaseResourceStatus.Assigned,
            tenantId,
            "protected-value",
            "tenant-v1",
            DateTime.UtcNow));
        var protector = new StubProtector("Server=internal;Database=monster-db-17;User Id=server-only;");
        var resolver = new TenantDatabaseResolver(reader, protector, NullLogger<TenantDatabaseResolver>.Instance);

        var result = await resolver.ResolveAsync(tenantId);

        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(resourceId, result.DatabaseResourceId);
        Assert.Equal("Server=internal;Database=monster-db-17;User Id=server-only;", result.ConnectionString);
        Assert.Equal("protected-value", protector.LastUnprotectedValue);
    }

    [Fact]
    public async Task Fails_closed_for_a_stale_or_cross_tenant_mapping()
    {
        var tenantId = Guid.NewGuid();
        var reader = new StubMappingReader(new TenantDatabaseMappingRecord(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            "ManualMonster",
            "monster-db-17",
            DatabaseResourceStatus.Provisioning,
            tenantId,
            "protected-value",
            null,
            null));
        var protector = new StubProtector("should-not-be-read");
        var resolver = new TenantDatabaseResolver(reader, protector, NullLogger<TenantDatabaseResolver>.Instance);

        var result = await resolver.ResolveAsync(tenantId);

        Assert.Null(result);
        Assert.Null(protector.LastUnprotectedValue);
    }

    [Fact]
    public async Task Rejects_empty_tenant_id_before_querying_platform_db()
    {
        var reader = new StubMappingReader(null);
        var resolver = new TenantDatabaseResolver(reader, new StubProtector("unused"), NullLogger<TenantDatabaseResolver>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolveAsync(Guid.Empty));
        Assert.False(reader.WasCalled);
    }

    private sealed class StubMappingReader(TenantDatabaseMappingRecord? record) : ITenantDatabaseMappingReader
    {
        public bool WasCalled { get; private set; }

        public Task<TenantDatabaseMappingRecord?> FindActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(record);
        }
    }

    private sealed class StubProtector(string value) : IConnectionStringProtector
    {
        public string? LastUnprotectedValue { get; private set; }

        public string Protect(string connectionString) => connectionString;

        public string Unprotect(string protectedConnectionString)
        {
            LastUnprotectedValue = protectedConnectionString;
            return value;
        }
    }
}
