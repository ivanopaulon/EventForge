using EventForge.Server.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace EventForge.Server.Middleware;

/// <summary>
/// Global middleware for centralized exception handling.
/// Intercepts ALL unhandled exceptions and converts them to RFC 7807 Problem Details responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught by GlobalExceptionHandler");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if response has already started - if so, we can't modify it
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot handle exception - response has already started");
            return;
        }

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

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
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
            case NotFoundException notFoundEx:
                _logger.LogWarning(notFoundEx, "Entity not found: {Message}", notFoundEx.Message);
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Extensions["entityName"] = notFoundEx.EntityName;
                problemDetails.Extensions["entityId"] = notFoundEx.EntityId;
                break;

            case ConflictException conflictEx:
                _logger.LogWarning(conflictEx, "Conflict: {Message}", conflictEx.Message);
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                problemDetails.Title = "Conflict";
                problemDetails.Detail = conflictEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                _logger.LogWarning(forbiddenEx, "Access forbidden: {Message}", forbiddenEx.Message);
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = forbiddenEx.Message;
                break;

            case BusinessValidationException businessEx:
                _logger.LogWarning(businessEx, "Business validation failed: {Message}", businessEx.Message);
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Business Validation Error";
                problemDetails.Detail = _environment.IsDevelopment() ? businessEx.Message : "A business validation error occurred.";
                problemDetails.Extensions["errorCode"] = businessEx.ErrorCode;
                
                if (businessEx.ValidationErrors != null && businessEx.ValidationErrors.Any())
                {
                    problemDetails.Extensions["errors"] = businessEx.ValidationErrors;
                }
                break;

            case ValidationException validationEx:
                _logger.LogWarning(validationEx, "Validation failed: {Message}", validationEx.Message);
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Validation Error";
                problemDetails.Detail = "One or more validation errors occurred.";
                
                var errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                
                problemDetails.Extensions["errors"] = errors;
                break;

            case ArgumentNullException nullEx:
                _logger.LogWarning(nullEx, "Argument null: {ParamName}", nullEx.ParamName);
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = _environment.IsDevelopment() 
                    ? $"Required parameter '{nullEx.ParamName}' was null or missing"
                    : "A required parameter was null or missing";
                break;

            case ArgumentException argEx:
                _logger.LogWarning(argEx, "Invalid argument: {Message}", argEx.Message);
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = _environment.IsDevelopment() ? argEx.Message : "Invalid argument provided";
                break;

            case InvalidOperationException invalidEx:
                _logger.LogWarning(invalidEx, "Invalid operation: {Message}", invalidEx.Message);
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Invalid Operation";
                problemDetails.Detail = _environment.IsDevelopment() ? invalidEx.Message : "The requested operation is invalid";
                break;

            case UnauthorizedAccessException unauthorizedEx:
                _logger.LogWarning(unauthorizedEx, "Unauthorized access: {Message}", unauthorizedEx.Message);
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "You are not authorized to access this resource";
                break;

            case KeyNotFoundException notFoundKeyEx:
                _logger.LogWarning(notFoundKeyEx, "Key not found: {Message}", notFoundKeyEx.Message);
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Detail = _environment.IsDevelopment() ? notFoundKeyEx.Message : "The requested resource was not found";
                break;

            case TimeoutException timeoutEx:
                _logger.LogWarning(timeoutEx, "Request timeout: {Message}", timeoutEx.Message);
                problemDetails.Status = (int)HttpStatusCode.RequestTimeout;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7";
                problemDetails.Title = "Request Timeout";
                problemDetails.Detail = _environment.IsDevelopment() ? timeoutEx.Message : "The request timed out";
                break;

            default:
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred";

                // Only include exception details in development
                if (_environment.IsDevelopment())
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
/// Extension methods for registering GlobalExceptionHandlerMiddleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    /// <summary>
    /// Adds the Global Exception Handler middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
