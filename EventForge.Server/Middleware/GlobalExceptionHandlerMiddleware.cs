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
            LogException(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Logs the exception once at the appropriate level with full context.
    /// Validation/business errors are logged at Warning; everything else at Error.
    /// </summary>
    private void LogException(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : "N/A";
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();
        var queryString = context.Request.QueryString.ToString();
        var exceptionType = exception.GetType().Name;

        var userName = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            : null;
        var userId   = context.User?.FindFirst("user_id")?.Value;
        var tenantId = context.User?.FindFirst("tenant_id")?.Value;

        switch (exception)
        {
            case ValidationException validationEx:
            {
                var errorsByField = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => new { e.ErrorMessage, e.AttemptedValue, e.ErrorCode }).ToArray()
                    );

                _logger.LogWarning(
                    "Validation failed for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. " +
                    "Errors: {@ValidationErrors}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId,
                    errorsByField);
                break;
            }

            case BusinessValidationException businessEx:
            {
                _logger.LogWarning(
                    "Business validation failed for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. " +
                    "ErrorCode: {ErrorCode}, Message: {Message}, Errors: {@ValidationErrors}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId,
                    businessEx.ErrorCode, businessEx.Message, businessEx.ValidationErrors);
                break;
            }

            case NotFoundException notFoundEx:
                _logger.LogWarning(
                    "Entity not found for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. " +
                    "Entity: {EntityName}, Id: {EntityId}, Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId,
                    notFoundEx.EntityName, notFoundEx.EntityId, notFoundEx.Message);
                break;

            case ConflictException conflictEx:
                _logger.LogWarning(
                    "Conflict for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, conflictEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                _logger.LogWarning(
                    "Access forbidden for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, forbiddenEx.Message);
                break;

            case ArgumentNullException nullEx:
                _logger.LogWarning(
                    "Argument null for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. ParamName: {ParamName}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, nullEx.ParamName);
                break;

            case ArgumentException argEx:
                _logger.LogWarning(
                    "Invalid argument for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, argEx.Message);
                break;

            case InvalidOperationException invalidEx:
                _logger.LogWarning(
                    "Invalid operation for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, invalidEx.Message);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                _logger.LogWarning(
                    "Unauthorized access for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, unauthorizedEx.Message);
                break;

            case KeyNotFoundException keyNotFoundEx:
                _logger.LogWarning(
                    "Key not found for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, keyNotFoundEx.Message);
                break;

            case TimeoutException timeoutEx:
                _logger.LogWarning(
                    "Request timeout for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, timeoutEx.Message);
                break;

            default:
                _logger.LogError(exception,
                    "Unhandled exception for {Method} {Path}{QueryString} [{ExceptionType}] User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. Message: {Message}",
                    method, path, queryString, exceptionType,
                    userName ?? "Anonymous", userId ?? "N/A", tenantId ?? "N/A", correlationId, exception.Message);
                break;
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
        problemDetails.Extensions["method"] = context.Request.Method;

        switch (exception)
        {
            case NotFoundException notFoundEx:
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Extensions["entityName"] = notFoundEx.EntityName;
                problemDetails.Extensions["entityId"] = notFoundEx.EntityId;
                break;

            case ConflictException conflictEx:
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                problemDetails.Title = "Conflict";
                problemDetails.Detail = conflictEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = forbiddenEx.Message;
                break;

            case BusinessValidationException businessEx:
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
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = _environment.IsDevelopment()
                    ? $"Required parameter '{nullEx.ParamName}' was null or missing"
                    : "A required parameter was null or missing";
                break;

            case ArgumentException argEx:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = _environment.IsDevelopment() ? argEx.Message : "Invalid argument provided";
                break;

            case InvalidOperationException invalidEx:
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Invalid Operation";
                problemDetails.Detail = _environment.IsDevelopment() ? invalidEx.Message : "The requested operation is invalid";
                break;

            case UnauthorizedAccessException:
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "You are not authorized to access this resource";
                break;

            case KeyNotFoundException notFoundKeyEx:
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Detail = _environment.IsDevelopment() ? notFoundKeyEx.Message : "The requested resource was not found";
                break;

            case TimeoutException timeoutEx:
                problemDetails.Status = (int)HttpStatusCode.RequestTimeout;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.7";
                problemDetails.Title = "Request Timeout";
                problemDetails.Detail = _environment.IsDevelopment() ? timeoutEx.Message : "The request timed out";
                break;

            default:
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
