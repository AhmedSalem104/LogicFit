using LogicFit.API.Features.Platform.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformTenantEndpointContractTests
{
    [Fact]
    public void Admin_tenant_lifecycle_endpoints_are_present_on_the_platform_controller()
    {
        var controllerRoute = typeof(PlatformTenantsController)
            .GetCustomAttribute<RouteAttribute>()
            ?.Template;

        Assert.Equal("api/platform/tenants", controllerRoute);

        var routes = typeof(PlatformTenantsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method
                .GetCustomAttributes<HttpMethodAttribute>()
                .SelectMany(attribute => attribute.HttpMethods.Select(httpMethod =>
                {
                    var actionRoute = string.IsNullOrWhiteSpace(attribute.Template)
                        ? controllerRoute!
                        : $"{controllerRoute}/{attribute.Template}";
                    return $"{httpMethod} {actionRoute}";
                })))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("GET api/platform/tenants/{id:guid}/credentials", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/credentials/reset", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/soft-delete", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/restore", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/permanent-delete", routes);
    }
}
