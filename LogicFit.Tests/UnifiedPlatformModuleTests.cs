using LogicFit.API.Features.Platform.Auth;
using LogicFit.API.Features.Platform.WorkspaceApplications;
using Xunit;

namespace LogicFit.Tests;

public class UnifiedPlatformModuleTests
{
    [Fact]
    public void Platform_controllers_are_compiled_into_the_unified_api_host()
    {
        Assert.Equal("LogicFit.API", typeof(PlatformAuthController).Assembly.GetName().Name);
        Assert.Equal("LogicFit.API", typeof(PlatformWorkspaceApplicationsController).Assembly.GetName().Name);
    }
}
