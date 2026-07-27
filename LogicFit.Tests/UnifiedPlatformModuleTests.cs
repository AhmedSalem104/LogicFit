using LogicFit.API.Features.Platform.Auth;
using Xunit;

namespace LogicFit.Tests;

public class UnifiedPlatformModuleTests
{
    [Fact]
    public void Platform_controllers_are_compiled_into_the_unified_api_host()
    {
        Assert.Equal("LogicFit.API", typeof(PlatformAuthController).Assembly.GetName().Name);
    }
}
