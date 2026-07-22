using System.Text.Json;
using LogicFit.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LogicFit.API.Authorization;

/// <summary>
/// Emits a typed <c>TENANT_PENDING_APPROVAL</c> (403) body when authorization fails specifically on the
/// <see cref="ActiveTenantRequirement"/> (i.e. a PendingApproval gym hit a non-billing endpoint).
/// All other authorization results fall through to the default handler unchanged.
/// </summary>
public class TenantAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var failedOnTenant = authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<ActiveTenantRequirement>().Any() == true;

        if (failedOnTenant)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                statusCode = 403,
                code = "TENANT_PENDING_APPROVAL",
                message = "Your gym is pending approval. Only billing and onboarding actions are available until it is approved.",
                errors = (object?)null
            });
            await context.Response.WriteAsync(body);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
