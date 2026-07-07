using LogicFit.Domain.Authorization;
using Xunit;

namespace LogicFit.Tests;

public class PermissionsCatalogTests
{
    [Fact]
    public void All_Contains_Both_Tenant_And_Platform_Permissions()
    {
        Assert.Contains(Permissions.ManageMembers, Permissions.All);
        Assert.Contains(Permissions.ManagePlatform, Permissions.All);
        Assert.Equal(
            Permissions.TenantPermissions.Count + Permissions.PlatformPermissions.Count,
            Permissions.All.Count);
    }

    [Fact]
    public void Permission_Codes_Are_Unique()
    {
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
    }

    [Fact]
    public void ManagePlatform_Is_A_Platform_Permission_Only()
    {
        Assert.Contains(Permissions.ManagePlatform, Permissions.PlatformPermissions);
        Assert.DoesNotContain(Permissions.ManagePlatform, Permissions.TenantPermissions);
    }
}
