using Xunit;

namespace LogicFit.Tests;

public sealed class CoachScreenIsolationContractTests
{
    [Fact]
    public void Coach_client_reads_and_assignments_are_scoped_to_the_current_tenant()
    {
        var root = RepoRoot();
        var listHandler = Read(root, "LogicFit.Application", "Features", "CoachClients", "Queries", "GetCoachClients", "GetCoachClientsQueryHandler.cs");
        var assignHandler = Read(root, "LogicFit.Application", "Features", "CoachClients", "Commands", "AssignClientToCoach", "AssignClientToCoachCommandHandler.cs");
        var unassignHandler = Read(root, "LogicFit.Application", "Features", "CoachClients", "Commands", "UnassignClientFromCoach", "UnassignClientFromCoachCommandHandler.cs");

        Assert.Contains("u.TenantId == tenantId", listHandler, StringComparison.Ordinal);
        Assert.Contains(".Where(cc => cc.TenantId == tenantId)", listHandler, StringComparison.Ordinal);
        Assert.Contains("u.TenantId == tenantId", assignHandler, StringComparison.Ordinal);
        Assert.Contains("cc.TenantId == tenantId", assignHandler, StringComparison.Ordinal);
        Assert.Contains("cc.TenantId == tenantId", unassignHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void Coach_measurements_require_workspace_and_active_coach_client_scope()
    {
        var root = RepoRoot();
        var query = Read(root, "LogicFit.Application", "Features", "BodyMeasurements", "Queries", "GetBodyMeasurements", "GetBodyMeasurementsQueryHandler.cs");
        var create = Read(root, "LogicFit.Application", "Features", "BodyMeasurements", "Commands", "CreateBodyMeasurement", "CreateBodyMeasurementCommandHandler.cs");

        Assert.Contains("u.TenantId == tenantId", query, StringComparison.Ordinal);
        Assert.Contains("_context.CoachClients.Any", query, StringComparison.Ordinal);
        Assert.Contains("u.TenantId == tenantId", create, StringComparison.Ordinal);
        Assert.Contains("_context.CoachClients.AnyAsync", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Challenge_reads_and_writes_reject_cross_tenant_records()
    {
        var root = RepoRoot();
        var challengeQuery = Read(root, "LogicFit.Application", "Features", "Challenges", "Queries", "GetChallenges", "GetChallengesQueryHandler.cs");
        var challengeDetails = Read(root, "LogicFit.Application", "Features", "Challenges", "Queries", "GetChallengeById", "GetChallengeByIdQueryHandler.cs");
        var leaderboard = Read(root, "LogicFit.Application", "Features", "Challenges", "Queries", "GetChallengeLeaderboard", "GetChallengeLeaderboardQueryHandler.cs");
        var join = Read(root, "LogicFit.Application", "Features", "Challenges", "Commands", "JoinChallenge", "JoinChallengeCommandHandler.cs");
        var progress = Read(root, "LogicFit.Application", "Features", "Challenges", "Commands", "UpdateProgress", "UpdateProgressCommandHandler.cs");
        var create = Read(root, "LogicFit.Application", "Features", "Challenges", "Commands", "CreateChallenge", "CreateChallengeCommandHandler.cs");

        Assert.Contains("c.TenantId == tenantId", challengeQuery, StringComparison.Ordinal);
        Assert.Contains("c.TenantId == tenantId", challengeDetails, StringComparison.Ordinal);
        Assert.Contains("cc.TenantId == tenantId", leaderboard, StringComparison.Ordinal);
        Assert.Contains("c.TenantId == tenantId", join, StringComparison.Ordinal);
        Assert.Contains("cc.TenantId == tenantId", progress, StringComparison.Ordinal);
        Assert.Contains("u.TenantId == tenantId", create, StringComparison.Ordinal);
        Assert.Contains("validClientIds", create, StringComparison.Ordinal);
    }

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
}
