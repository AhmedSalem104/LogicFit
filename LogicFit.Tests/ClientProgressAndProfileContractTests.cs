using Xunit;

namespace LogicFit.Tests;

public sealed class ClientProgressAndProfileContractTests
{
    [Fact]
    public void Client_progress_has_a_self_scoped_endpoint_and_report_handler()
    {
        var root = RepoRoot();
        var controller = Read(root, "LogicFit.Api", "Features", "ClientDashboard", "ClientDashboardController.cs");
        var handler = Read(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetTraineeProgressReport", "GetTraineeProgressReportQueryHandler.cs");

        Assert.Contains("[HttpGet(\"my-progress\")]", controller, StringComparison.Ordinal);
        Assert.Contains("GetTraineeProgressReportQuery", controller, StringComparison.Ordinal);
        Assert.Contains("currentUser.Role == UserRole.Client", handler, StringComparison.Ordinal);
        Assert.Contains("Clients can only view their own progress", handler, StringComparison.Ordinal);
        Assert.Contains("bm.TenantId == tenantId", handler, StringComparison.Ordinal);
        Assert.Contains("ws.TenantId == tenantId", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Self_profile_reads_and_updates_are_tenant_scoped_and_support_phone_validation()
    {
        var root = RepoRoot();
        var query = Read(root, "LogicFit.Application", "Features", "Profile", "Queries", "GetMyProfile", "GetMyProfileQueryHandler.cs");
        var command = Read(root, "LogicFit.Application", "Features", "Profile", "Commands", "UpdateMyProfile", "UpdateMyProfileCommand.cs");
        var handler = Read(root, "LogicFit.Application", "Features", "Profile", "Commands", "UpdateMyProfile", "UpdateMyProfileCommandHandler.cs");

        Assert.Contains("u.TenantId == tenantId", query, StringComparison.Ordinal);
        Assert.Contains("PhoneNumber", command, StringComparison.Ordinal);
        Assert.Contains("u.TenantId == tenantId", handler, StringComparison.Ordinal);
        Assert.Contains("phoneInUse", handler, StringComparison.Ordinal);
    }

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
}
