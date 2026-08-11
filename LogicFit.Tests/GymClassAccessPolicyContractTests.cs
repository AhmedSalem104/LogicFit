using LogicFit.Domain.Authorization;
using Xunit;

namespace LogicFit.Tests;

public sealed class GymClassAccessPolicyContractTests
{
    [Fact]
    public void Group_class_and_schedule_controllers_require_branch_management()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var groupClasses = File.ReadAllText(Path.Combine(root, "LogicFit.API", "Features", "GroupClasses", "GroupClassesController.cs"));
        var schedules = File.ReadAllText(Path.Combine(root, "LogicFit.API", "Features", "ClassSchedules", "ClassSchedulesController.cs"));

        Assert.Contains("[Authorize(Policy = Permissions.ManageBranches)]", groupClasses, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = Permissions.ManageBranches)]", schedules, StringComparison.Ordinal);
        Assert.Equal("ManageBranches", Permissions.ManageBranches);
    }
}
