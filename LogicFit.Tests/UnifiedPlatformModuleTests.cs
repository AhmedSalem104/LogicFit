using LogicFit.API.Features.Platform.Auth;
using LogicFit.API.Features.Platform.WorkspaceApplications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
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

    [Fact]
    public void Platform_login_is_email_password_only_and_phone_otp_routes_are_not_exposed()
    {
        var methods = typeof(PlatformAuthController).GetMethods();
        var login = Assert.Single(methods, method =>
            method.GetCustomAttributes<HttpPostAttribute>().Any(attribute => attribute.Template == "login"));

        Assert.Contains("PlatformPasswordLoginCommand", login.GetParameters().Single().ParameterType.Name,
            StringComparison.Ordinal);
        Assert.DoesNotContain(methods.SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()),
            attribute => (attribute.Template ?? string.Empty).Contains("otp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods.SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()),
            attribute => (attribute.Template ?? string.Empty).Contains("phone", StringComparison.OrdinalIgnoreCase));
    }
}
