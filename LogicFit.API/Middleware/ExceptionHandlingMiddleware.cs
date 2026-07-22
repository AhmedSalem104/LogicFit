using System.Text.Json;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors, code) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validationEx.Errors,
                (string?)null
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            // A gym that isn't allowed to be served (suspended/expired/...). Carries a typed code.
            TenantAccessException tenantEx => (
                tenantEx.StatusCode,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)tenantEx.Code
            ),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            SubscriptionLimitException => (
                StatusCodes.Status402PaymentRequired,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            ConflictException => (
                StatusCodes.Status409Conflict,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The record was changed by another request. Please reload and try again.",
                (IDictionary<string, string[]>?)null,
                (string?)"CONCURRENCY_CONFLICT"
            ),
            // Base domain/business-rule violations (insufficient stock, coupon limits, etc.) are bad
            // requests, not server errors. Keep this AFTER the more specific subclasses above.
            DomainException => (
                StatusCodes.Status400BadRequest,
                exception.Message,
                (IDictionary<string, string[]>?)null,
                (string?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request",
                (IDictionary<string, string[]>?)null,
                (string?)null
            )
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            statusCode,
            code,
            message,
            errors
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(response, options);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
