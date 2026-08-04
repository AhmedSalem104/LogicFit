using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantDatabasePurgeContractTests
{
    [Fact]
    public async Task Monster_provider_fails_closed_and_never_purges_from_the_api_path()
    {
        var provider = new ManualMonsterTenantDatabasePurgeProvider();
        var request = new TenantDatabasePurgeRequest(Guid.NewGuid(), Guid.NewGuid(), "ManualMonster");

        var capabilities = provider.GetCapabilities();
        var result = await provider.PurgeAsync(request);

        Assert.False(capabilities.Enabled);
        Assert.Equal("ManualOnly", capabilities.Mode);
        Assert.False(result.Succeeded);
        Assert.Equal("TENANT_DATABASE_PURGE_MANUAL_ONLY", result.ErrorCode);
    }
}
