using Xunit;

namespace LogicFit.Tests;

public sealed class ApiEndpointCatalogTests
{
    [Fact]
    public void Catalog_preserves_absolute_action_routes_without_controller_prefixes()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var catalog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "API-ENDPOINT-CATALOG.md"));

        Assert.Contains("POST /api/freelance/team/invites", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "POST /api/freelance/team/applications/api/freelance/team/invites",
            catalog,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_generator_handles_absolute_http_action_templates()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var generator = File.ReadAllText(Path.Combine(repositoryRoot, "Scripts", "Export-ApiEndpointCatalog.ps1"));

        Assert.Contains("$routeSuffix.StartsWith('/')", generator, StringComparison.Ordinal);
        Assert.Contains("ignores the controller prefix", generator, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_documents_business_value_and_safe_response_fields()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var catalog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "API-ENDPOINT-CATALOG.md"));

        Assert.Contains("**Business benefit:**", catalog, StringComparison.Ordinal);
        Assert.Contains("**Response schema:**", catalog, StringComparison.Ordinal);
        Assert.Contains("POST /api/platform/workspace-applications", catalog, StringComparison.Ordinal);
        Assert.Contains("PlatformWorkspaceApplicationCreatedDto", catalog, StringComparison.Ordinal);
        Assert.Contains("AuthResponseDto", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("`RefreshToken`", catalog, StringComparison.Ordinal);
    }
}
