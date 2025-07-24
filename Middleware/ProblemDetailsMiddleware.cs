using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace EventForge.Middleware;

/// <summary>
/// Middleware for handling exceptions and converting them to ProblemDetails responses (RFC7807).
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var problemDetails = CreateProblemDetails(context, exception);

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        // Add correlation ID as an extension
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        switch (exception)
        {
            case ArgumentNullException nullEx:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = $"Required parameter '{nullEx.ParamName}' was null or missing";
                break;

            case ArgumentException argEx:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = argEx.Message;
                break;

            case InvalidOperationException invalidEx:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Invalid Operation";
                problemDetails.Detail = invalidEx.Message;
                break;

            case UnauthorizedAccessException:
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "You are not authorized to access this resource";
                break;

            case KeyNotFoundException notFoundEx:
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Detail = notFoundEx.Message;
                break;

            case TimeoutException timeoutEx:
                problemDetails.Status = (int)HttpStatusCode.RequestTimeout;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7";
                problemDetails.Title = "Request Timeout";
                problemDetails.Detail = timeoutEx.Message;
                break;

            default:
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred";

                // Only include exception details in development
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                if (isDevelopment)
                {
                    problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                    problemDetails.Extensions["exceptionMessage"] = exception.Message;
                    problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
                break;
        }

        return problemDetails;
    }
}

/// <summary>
/// Extension methods for registering ProblemDetailsMiddleware.
/// </summary>
public static class ProblemDetailsMiddlewareExtensions
{
    /// <summary>
    /// Adds the ProblemDetails middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProblemDetailsMiddleware>();
    }
}