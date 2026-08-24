using Xunit;

namespace LogicFit.Tests;

public sealed class WorkspaceApplicationInformationContractTests
{
    [Fact]
    public void Workspace_creation_requests_use_the_shared_payload_whitelist()
    {
        var handler = Read(
            "LogicFit.Application",
            "Features",
            "WorkspaceApplications",
            "Commands",
            "RequestApplicationInformation",
            "RequestApplicationInformationCommandHandler.cs");

        Assert.Contains(
            "application.ApplicationType is ApplicationType.FreelanceWorkspaceCreation or ApplicationType.GymWorkspaceCreation",
            handler,
            StringComparison.Ordinal);
        Assert.Contains("FreelanceWorkspaceApplicationFields.AreAllowed(fields)", handler, StringComparison.Ordinal);
        Assert.Contains("fields.All(x => x == \"FullName\")", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_whitelist_contains_the_fields_used_by_the_shared_application_payload()
    {
        var fields = Read(
            "LogicFit.Application",
            "Features",
            "WorkspaceApplications",
            "FreelanceWorkspaceApplicationFields.cs");

        Assert.Contains("\"WorkspaceName\"", fields, StringComparison.Ordinal);
        Assert.Contains("\"BrandName\"", fields, StringComparison.Ordinal);
        Assert.Contains("\"Bio\"", fields, StringComparison.Ordinal);
        Assert.Contains("\"PaymentProof\"", fields, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Address\"", fields, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}
