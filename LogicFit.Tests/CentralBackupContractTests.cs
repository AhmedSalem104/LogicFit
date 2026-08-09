using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class CentralBackupContractTests
{
    [Fact]
    public void Backup_scopes_are_explicit_and_do_not_include_a_shared_database_fallback()
    {
        Assert.Equal(6, Enum.GetValues<BackupScope>().Length);
        Assert.DoesNotContain("Shared", Enum.GetNames<BackupScope>());
    }

    [Fact]
    public void Batch_request_carries_only_server_resolved_target_identifiers()
    {
        var tenantId = Guid.NewGuid();
        var request = new BackupBatchRequest(BackupScope.SelectedTenants, [tenantId], "retry-1");

        Assert.Equal(BackupScope.SelectedTenants, request.Scope);
        Assert.Equal([tenantId], request.TenantIds);
        Assert.DoesNotContain("Connection", string.Join(',', request.TenantIds!));
    }

    [Fact]
    public void Backup_artifact_contract_exposes_checksum_without_connection_material()
    {
        var artifact = new BackupArtifactDto(
            Guid.NewGuid(), null, "Completed", 42, null, DateTimeOffset.UtcNow,
            "platform-20260805-010203.bacpac", "ABC123", null);

        Assert.Equal("ABC123", artifact.Sha256);
        Assert.DoesNotContain("Connection", string.Join(',', artifact.GetType().GetProperties().Select(x => x.Name)));
    }

    [Fact]
    public void Full_system_requests_include_platform_by_default()
    {
        var request = new BackupBatchRequest(BackupScope.FullSystem);

        Assert.True(request.IncludePlatform);
    }

    [Fact]
    public void Backup_service_records_start_and_finish_audit_events()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(repositoryRoot, "LogicFit.Infrastructure", "Services", "DatabaseBackupService.cs"));

        Assert.Contains("PlatformBackupBatchStarted", source, StringComparison.Ordinal);
        Assert.Contains("PlatformBackupBatchFinished", source, StringComparison.Ordinal);
        Assert.Contains("failedArtifacts", source, StringComparison.Ordinal);
        Assert.Contains("IncludePlatform", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("platform-20260805-010203.bacpac")]
    [InlineData("tenant-db-20260805-010203-0123456789abcdef0123456789abcdef.bacpac")]
    [InlineData("manifest-0123456789abcdef0123456789abcdef.json")]
    public void Generated_backup_and_manifest_keys_are_downloadable(string fileName)
    {
        Assert.True(BackupFileNamePolicy.IsSafe(fileName));
    }

    [Theory]
    [InlineData("../manifest-0123456789abcdef0123456789abcdef.json")]
    [InlineData("tenant-db-20260805-010203-0123456789abcdef.bacpac")]
    public void Backup_download_policy_rejects_unsafe_or_incomplete_keys(string fileName)
    {
        Assert.False(BackupFileNamePolicy.IsSafe(fileName));
    }

    [Fact]
    public void Full_system_retry_with_an_empty_explicit_tenant_set_keeps_only_the_platform_target()
    {
        var request = new BackupBatchRequest(BackupScope.FullSystem, Array.Empty<Guid>(), IncludePlatform: true);

        Assert.False(DatabaseBackupService.IsTenantIncluded(request, Guid.NewGuid()));
    }

    [Fact]
    public void Full_system_without_an_explicit_tenant_set_still_includes_all_tenants()
    {
        var request = new BackupBatchRequest(BackupScope.FullSystem, TenantIds: null, IncludePlatform: true);

        Assert.True(DatabaseBackupService.IsTenantIncluded(request, Guid.NewGuid()));
    }
}
