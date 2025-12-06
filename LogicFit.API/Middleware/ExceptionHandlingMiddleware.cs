using System.Text.Json;
using LogicFit.Domain.Exceptions;

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

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validationEx.Errors
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                exception.Message,
                (IDictionary<string, string[]>?)null
            ),
            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                exception.Message,
                (IDictionary<string, string[]>?)null
            ),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                exception.Message,
                (IDictionary<string, string[]>?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request",
                (IDictionary<string, string[]>?)null
            )
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            statusCode,
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
