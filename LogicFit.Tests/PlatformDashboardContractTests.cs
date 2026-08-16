using LogicFit.API.Features.Platform.DatabaseResources;
using LogicFit.API.Features.Platform.Diagnostics;
using LogicFit.Application.Features.Platform.Dashboard;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformDashboardContractTests
{
    [Fact]
    public void Dashboard_operations_summary_is_safe_and_starts_empty()
    {
        var dto = new PlatformDashboardDto();

        Assert.NotNull(dto.Operations);
        Assert.Equal(0, dto.Operations.DatabasePool.Total);
        Assert.Equal(0, dto.Operations.Applications.Submitted);
        Assert.False(dto.Operations.Restores.Capabilities.Enabled);
    }

    [Fact]
    public void Database_resource_contract_exposes_safe_name_but_not_connection_material()
    {
        var propertyNames = typeof(PlatformDatabaseResourceDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("DatabaseName", propertyNames);
        Assert.Contains("ServerHost", propertyNames);
        Assert.Contains("LastConnectionErrorCode", propertyNames);
        Assert.DoesNotContain("ConnectionString", propertyNames);
        Assert.DoesNotContain("EncryptedConnectionString", propertyNames);

        var protectedConnectionProperty = typeof(PlatformDatabaseResourceDto).GetProperty("HasProtectedConnection");
        Assert.NotNull(protectedConnectionProperty);
        Assert.Equal(typeof(bool), protectedConnectionProperty!.PropertyType);
    }

    [Fact]
    public void Database_resource_list_requires_platform_backup_permission()
    {
        var authorize = typeof(PlatformDatabaseResourcesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>();

        Assert.Contains(authorize, attribute => attribute.Policy == Permissions.ManagePlatformBackups);
    }

    [Fact]
    public void Database_resource_console_exposes_guarded_operational_actions()
    {
        var methodNames = typeof(PlatformDatabaseResourcesController)
            .GetMethods()
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("TestConnection", methodNames);
        Assert.Contains("TestStoredConnection", methodNames);
        Assert.Contains("RepairConnection", methodNames);
        Assert.Contains("RunMigrations", methodNames);
        Assert.Contains("CreateBackup", methodNames);
        Assert.Contains("SetStatus", methodNames);
        Assert.Contains("Delete", methodNames);
    }

    [Fact]
    public void Version_contract_truncates_build_sha_and_uses_explicit_api_contract_version()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:ContractVersion"] = "v2",
                ["Build:Sha"] = "1234567890abcdef"
            })
            .Build();
        var controller = new PlatformDiagnosticsController(configuration, new TestHostEnvironment());

        var result = controller.Version().Result;
        var value = Assert.IsType<PlatformVersionDiagnosticsDto>(Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result).Value);

        Assert.Equal("v2", value.ApiContractVersion);
        Assert.Equal("1234567890ab", value.BuildSha);
        Assert.DoesNotContain("1234567890abcdef", value.BuildSha);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "LogicFit.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
