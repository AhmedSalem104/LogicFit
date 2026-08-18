using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformWorkflowContextBoundaryTests
{
    [Fact]
    public void Resource_pool_uses_the_platform_context()
    {
        var constructor = typeof(DatabaseResourcePoolService).GetConstructors().Single();

        Assert.Equal(typeof(PlatformDbContext), constructor.GetParameters()[0].ParameterType);
        Assert.DoesNotContain(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(ApplicationDbContext));
    }

    [Fact]
    public void Workspace_provisioning_saga_uses_the_platform_context()
    {
        var constructor = typeof(WorkspaceProvisioningSaga).GetConstructors().Single();

        Assert.Equal(typeof(PlatformDbContext), constructor.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(ApplicationDbContext), constructor.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void Monster_provisioning_runtime_constructor_uses_the_platform_context()
    {
        var constructor = typeof(ManualMonsterProvisioningProvider)
            .GetConstructors()
            .Single(constructor => constructor.GetCustomAttributes(typeof(ActivatorUtilitiesConstructorAttribute), inherit: false).Length != 0);

        Assert.Equal(typeof(PlatformDbContext), constructor.GetParameters()[1].ParameterType);
        Assert.DoesNotContain(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(ApplicationDbContext));
    }
}
