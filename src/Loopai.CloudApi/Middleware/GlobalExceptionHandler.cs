using System.Net;
using System.Text.Json;
using Loopai.CloudApi.DTOs;
using FluentValidation;

namespace Loopai.CloudApi.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var (statusCode, errorCode, message, details) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "Validation failed",
                (object)validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                "INVALID_ARGUMENT",
                argEx.Message,
                (object?)null
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.Conflict,
                "INVALID_OPERATION",
                invalidOpEx.Message,
                (object?)null
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "FORBIDDEN",
                "Access denied",
                (object?)null
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                "The requested resource was not found",
                (object?)null
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred",
                (object?)null
            )
        };

        var response = new ErrorResponse
        {
            Code = errorCode,
            Message = message,
            Details = details ?? (_environment.IsDevelopment() ? exception.Message : null),
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Extension methods for registering the global exception handler.
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}
