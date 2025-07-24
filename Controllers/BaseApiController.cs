using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// Base controller class providing common functionality for all API controllers.
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
}