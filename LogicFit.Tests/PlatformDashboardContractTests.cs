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
    public void Database_resource_contract_does_not_expose_connection_metadata()
    {
        var propertyNames = typeof(PlatformDatabaseResourceDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("DatabaseName", propertyNames);
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
    public void Tenant_list_uses_a_separate_unfiltered_member_count_query()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.Application",
            "Features",
            "Platform",
            "Tenants",
            "Queries",
            "GetPlatformTenants",
            "GetPlatformTenantsQueryHandler.cs"));

        Assert.Contains("var tenantRows", source);
        Assert.Contains("IgnoreQueryFilters()", source);
        Assert.Contains("GroupBy(u => u.TenantId)", source);
        Assert.DoesNotContain("MembersCount = _context.Users", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_tenant_summary_uses_a_separate_unfiltered_member_count_query()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.API",
            "Features",
            "Platform",
            "Dashboard",
            "PlatformDashboardController.cs"));

        Assert.Contains("var paged", source, StringComparison.Ordinal);
        Assert.Contains("GroupBy(user => user.TenantId)", source, StringComparison.Ordinal);
        Assert.Contains("IgnoreQueryFilters()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MembersCount = context.Users", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_application_list_chooses_the_latest_duplicate_payment_row()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.Application",
            "Features",
            "WorkspaceApplications",
            "Queries",
            "GetPlatformApplications",
            "GetPlatformApplicationsQueryHandler.cs"));

        Assert.Contains("GroupBy(x => x.ApplicationId)", source, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("var paymentsByApplication = payments.ToDictionary", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_resource_list_ignores_legacy_null_backup_resource_ids()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.API",
            "Features",
            "Platform",
            "DatabaseResources",
            "PlatformDatabaseResourcesController.cs"));

        Assert.Contains("x.DatabaseResourceId.HasValue && resourceIds.Contains(x.DatabaseResourceId.Value)", source, StringComparison.Ordinal);
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
