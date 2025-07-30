using Microsoft.AspNetCore.Mvc;
using EventForge.Server.Services.Tenants;

namespace EventForge.Server.Controllers;

/// <summary>
/// Base controller class providing common functionality for all API controllers.
/// Includes multi-tenant support, standardized error handling, and validation helpers.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Creates a ProblemDetails response for ModelState validation errors.
    /// </summary>
    /// <returns>BadRequest result with ProblemDetails</returns>
    protected ActionResult CreateValidationProblemDetails()
    {
        var problemDetails = new ValidationProblemDetails(ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        };

        // Add correlation ID if available
        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return BadRequest(problemDetails);
    }

    /// <summary>
    /// Gets the current user from the request context.
    /// For demonstration purposes, this returns a default user.
    /// In a real application, this would extract the user from JWT claims or similar.
    /// </summary>
    /// <returns>Current user identifier</returns>
    protected string GetCurrentUser()
    {
        // In a real application, you would extract this from JWT claims, session, etc.
        // For now, we'll use a default user
        return User?.Identity?.Name ?? "system";
    }

    /// <summary>
    /// Creates a ProblemDetails response for not found errors.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>NotFound result with ProblemDetails</returns>
    protected ActionResult CreateNotFoundProblem(string message)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = message,
            Instance = HttpContext.Request.Path
        };

        // Add correlation ID if available
        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return NotFound(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails response for internal server errors.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="ex">The exception</param>
    /// <returns>InternalServerError result with ProblemDetails</returns>
    protected ActionResult CreateInternalServerErrorProblem(string message, Exception ex)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = message,
            Instance = HttpContext.Request.Path
        };

        // Add correlation ID if available
        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // In development, include exception details
        if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            problemDetails.Extensions["exception"] = ex.Message;
            problemDetails.Extensions["stackTrace"] = ex.StackTrace;
        }

        return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
    }

    /// <summary>
    /// Validates current tenant context and ensures user has access to the tenant.
    /// </summary>
    /// <param name="tenantContext">The tenant context service</param>
    /// <returns>ActionResult with error if validation fails, null if validation passes</returns>
    protected async Task<ActionResult?> ValidateTenantAccessAsync(ITenantContext tenantContext)
    {
        if (tenantContext?.CurrentTenantId == null)
        {
            return CreateValidationProblemDetails("Tenant context is required for this operation.");
        }

        // Super admins can access any tenant when impersonating
        if (tenantContext.IsSuperAdmin)
        {
            return null;
        }

        // Regular users must have access to the current tenant
        var canAccess = await tenantContext.CanAccessTenantAsync(tenantContext.CurrentTenantId.Value);
        if (!canAccess)
        {
            return Forbid("You don't have access to this tenant.");
        }

        return null;
    }

    /// <summary>
    /// Creates a ProblemDetails response for validation errors with a custom message.
    /// </summary>
    /// <param name="message">The validation error message</param>
    /// <returns>BadRequest result with ProblemDetails</returns>
    protected ActionResult CreateValidationProblemDetails(string message)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = message,
            Instance = HttpContext.Request.Path
        };

        // Add correlation ID if available
        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return BadRequest(problemDetails);
    }

    /// <summary>
    /// Validates pagination parameters and returns standardized error if invalid.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="maxPageSize">Maximum allowed page size (default: 100)</param>
    /// <returns>ActionResult with error if validation fails, null if validation passes</returns>
    protected ActionResult? ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 100)
    {
        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > maxPageSize)
        {
            ModelState.AddModelError(nameof(pageSize), $"Page size must be between 1 and {maxPageSize}.");
        }

        return !ModelState.IsValid ? CreateValidationProblemDetails() : null;
    }
}