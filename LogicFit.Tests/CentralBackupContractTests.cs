using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
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
}
