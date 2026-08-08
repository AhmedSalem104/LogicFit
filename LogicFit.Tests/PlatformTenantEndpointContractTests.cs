using LogicFit.API.Features.Platform.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformTenantEndpointContractTests
{
    [Fact]
    public void Gym_lifecycle_and_credentials_routes_are_compiled_into_the_unified_api()
    {
        var controllerRoute = typeof(PlatformTenantsController)
            .GetCustomAttributes<RouteAttribute>()
            .Single()
            .Template;
        var routes = typeof(PlatformTenantsController)
            .GetMethods()
            .SelectMany(method => method
                .GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} {controllerRoute}/{attribute.Template}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("GET api/platform/tenants/{id:guid}/credentials", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/credentials/reset", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/soft-delete", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/restore", routes);
        Assert.Contains("POST api/platform/tenants/{id:guid}/permanent-delete", routes);
    }
}
