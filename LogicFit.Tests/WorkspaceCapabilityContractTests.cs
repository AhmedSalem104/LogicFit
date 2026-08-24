using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public sealed class WorkspaceCapabilityContractTests
{
    [Fact]
    public void Freelance_workspace_gets_coaching_capabilities_but_no_gym_surface()
    {
        var capabilities = WorkspaceCapabilities.For(WorkspaceType.FreelanceCoach);

        Assert.Contains(WorkspaceCapabilities.FreelanceExperience, capabilities);
        Assert.Contains(WorkspaceCapabilities.CoachingClients, capabilities);
        Assert.Contains(WorkspaceCapabilities.CoachingFinance, capabilities);
        Assert.Contains(WorkspaceCapabilities.FreelanceTeam, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymFacilities, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymAttendance, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymStaff, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymInventory, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymPos, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymGateAccess, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymMembershipCards, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.GymMembershipPlans, capabilities);
    }

    [Fact]
    public void Gym_workspace_keeps_gym_surface_and_shared_coaching()
    {
        var capabilities = WorkspaceCapabilities.For(WorkspaceType.Gym);

        Assert.Contains(WorkspaceCapabilities.GymExperience, capabilities);
        Assert.Contains(WorkspaceCapabilities.GymFacilities, capabilities);
        Assert.Contains(WorkspaceCapabilities.GymStaff, capabilities);
        Assert.Contains(WorkspaceCapabilities.GymInventory, capabilities);
        Assert.Contains(WorkspaceCapabilities.GymPos, capabilities);
        Assert.Contains(WorkspaceCapabilities.GymMembershipPlans, capabilities);
        Assert.Contains(WorkspaceCapabilities.CoachingPrograms, capabilities);
        Assert.DoesNotContain(WorkspaceCapabilities.FreelanceTeam, capabilities);
    }

    [Fact]
    public void Gym_only_controllers_are_capability_protected()
    {
        var root = RepoRoot();
        AssertCapability(root, "Branches", nameof(WorkspaceCapabilities.GymFacilities));
        AssertCapability(root, "Employees", nameof(WorkspaceCapabilities.GymStaff));
        AssertCapability(root, "Products", nameof(WorkspaceCapabilities.GymInventory));
        AssertCapability(root, "Sales", nameof(WorkspaceCapabilities.GymPos));
        AssertCapability(root, "GateAccess", nameof(WorkspaceCapabilities.GymGateAccess));
        AssertCapability(root, "MembershipCards", nameof(WorkspaceCapabilities.GymMembershipCards));
        AssertCapability(root, "GroupClasses", nameof(WorkspaceCapabilities.GymFacilities));
        AssertCapability(root, "ClassSchedules", nameof(WorkspaceCapabilities.GymFacilities));
    }

    [Fact]
    public void Freelance_owner_seed_does_not_reuse_all_tenant_permissions()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "LogicFit.Infrastructure", "Persistence", "RbacSeeder.cs"));
        var start = source.IndexOf("[SystemRoles.FreelanceOwner]", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = source.IndexOf("[SystemRoles.FreelanceCoach]", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var mapping = source[start..end];
        Assert.DoesNotContain("Permissions.TenantPermissions.ToArray()", mapping, StringComparison.Ordinal);
        Assert.Contains("Permissions.ManageFinance", mapping, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions.ManageInventory", mapping, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions.ManageBranches", mapping, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions.ManageAttendance", mapping, StringComparison.Ordinal);
    }

    private static void AssertCapability(string root, string controller, string capabilityProperty)
    {
        var path = Directory.GetFiles(Path.Combine(root, "LogicFit.API", "Features"), $"{controller}Controller.cs", SearchOption.AllDirectories).Single();
        var source = File.ReadAllText(path);
        Assert.Contains($"[Authorize(Policy = WorkspaceCapabilities.{capabilityProperty})]", source, StringComparison.Ordinal);
    }

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
