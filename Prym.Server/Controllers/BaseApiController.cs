using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

/// <summary>
/// Base controller class providing common functionality for all API controllers.
/// Includes multi-tenant support, standardized error handling, and validation helpers.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    // -------------------------------------------------------------------------
    //  Helper: build and enrich a ProblemDetails instance with correlation id
    //  and timestamp. All Create*Problem helpers delegate to this.
    // -------------------------------------------------------------------------

    private ProblemDetails BuildProblemDetails(string type, string title, int status, string detail)
    {
        ProblemDetails problem = new()
        {
            Type     = type,
            Title    = title,
            Status   = status,
            Detail   = detail,
            Instance = HttpContext.Request.Path
        };

        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            problem.Extensions["correlationId"] = correlationId;

        problem.Extensions["timestamp"] = GetCurrentTimestamp();
        return problem;
    }

    // -------------------------------------------------------------------------
    //  Current user / timestamp helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns the current user name from JWT claims, falling back to "System".</summary>
    protected string GetCurrentUser() => User?.Identity?.Name ?? "System";

    /// <summary>Returns the current UTC timestamp in ISO-8601 round-trip format.</summary>
    protected static string GetCurrentTimestamp() => DateTime.UtcNow.ToString("o");

    // -------------------------------------------------------------------------
    //  Standardised Problem-Details factory methods
    // -------------------------------------------------------------------------

    /// <summary>400 – ModelState validation errors (no message override).</summary>
    protected ActionResult CreateValidationProblemDetails()
    {
        ValidationProblemDetails problem = new(ModelState)
        {
            Type     = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title    = "One or more validation errors occurred.",
            Status   = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        };

        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            problem.Extensions["correlationId"] = correlationId;

        problem.Extensions["timestamp"] = GetCurrentTimestamp();
        return BadRequest(problem);
    }

    /// <summary>400 – single validation error message.</summary>
    protected ActionResult CreateValidationProblemDetails(string message) =>
        BadRequest(BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            "Validation Error",
            StatusCodes.Status400BadRequest,
            message));

    /// <summary>401 – unauthenticated / token invalid.</summary>
    protected ActionResult CreateUnauthorizedProblem(string message) =>
        Unauthorized(BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7235#section-3.1",
            "Unauthorized",
            StatusCodes.Status401Unauthorized,
            message));

    /// <summary>403 – authenticated but not authorised.</summary>
    protected ActionResult CreateForbiddenProblem(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            "Forbidden",
            StatusCodes.Status403Forbidden,
            message));

    /// <summary>404 – resource not found.</summary>
    protected ActionResult CreateNotFoundProblem(string message) =>
        NotFound(BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            "Not Found",
            StatusCodes.Status404NotFound,
            message));

    /// <summary>409 – state conflict (e.g. duplicate key, concurrency).</summary>
    protected ActionResult CreateConflictProblem(string message) =>
        StatusCode(StatusCodes.Status409Conflict, BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            "Conflict",
            StatusCodes.Status409Conflict,
            message));

    /// <summary>422 – semantically invalid request body.</summary>
    protected ActionResult CreateUnprocessableEntityProblem(string message) =>
        UnprocessableEntity(BuildProblemDetails(
            "https://tools.ietf.org/html/rfc4918#section-11.2",
            "Unprocessable Entity",
            StatusCodes.Status422UnprocessableEntity,
            message));

    /// <summary>
    /// 500 – unexpected server-side error.
    /// Always logs the full exception details (message, stack trace, inner exceptions) through the
    /// controller's own <see cref="ILogger"/> so that every unhandled controller exception is
    /// traceable in the application log. Exception details are also included in the response body
    /// when running in the Development environment.
    /// </summary>
    protected ActionResult CreateInternalServerErrorProblem(string message, Exception ex)
    {
        // Resolve a logger scoped to the concrete controller type so that log categories are meaningful.
        var logger = HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType().FullName!);

        logger.LogError(
            ex,
            "Unhandled exception in {Controller} [{Method} {Path}] — {UserMessage}. CorrelationId: {CorrelationId}",
            GetType().Name,
            HttpContext.Request.Method,
            HttpContext.Request.Path,
            message,
            HttpContext.Items.TryGetValue("CorrelationId", out var cid) ? cid : "n/a");

        var problem = BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            "Internal Server Error",
            StatusCodes.Status500InternalServerError,
            message);

        if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            problem.Extensions["exception"]  = ex.Message;
            problem.Extensions["stackTrace"] = ex.StackTrace;
            problem.Extensions["innerException"] = ex.InnerException?.Message;
        }

        return StatusCode(StatusCodes.Status500InternalServerError, problem);
    }

    /// <summary>503 – downstream dependency unavailable.</summary>
    protected ActionResult CreateServiceUnavailableProblem(string message) =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, BuildProblemDetails(
            "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            "Service Unavailable",
            StatusCodes.Status503ServiceUnavailable,
            message));

    // -------------------------------------------------------------------------
    //  Tenant / pagination helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates that the current user has access to the active tenant.
    /// Returns a non-null <see cref="ActionResult"/> (400 or 403) when access is denied.
    /// </summary>
    protected async Task<ActionResult?> ValidateTenantAccessAsync(ITenantContext tenantContext)
    {
        if (tenantContext?.CurrentTenantId is null)
            return CreateValidationProblemDetails("Tenant context is required for this operation.");

        // Super admins can access any tenant when impersonating
        if (tenantContext.IsSuperAdmin)
            return null;

        // Regular users must have access to the current tenant
        var canAccess = await tenantContext.CanAccessTenantAsync(tenantContext.CurrentTenantId.Value);
        return canAccess ? null : CreateForbiddenProblem("You don't have access to this tenant.");
    }

    /// <summary>
    /// Validates pagination parameters and returns a 400 response when invalid.
    /// Returns <see langword="null"/> when parameters are valid.
    /// </summary>
    protected ActionResult? ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 100)
    {
        if (page < 1)
            ModelState.AddModelError(nameof(page), "Page number must be greater than 0.");

        if (pageSize < 1 || pageSize > maxPageSize)
            ModelState.AddModelError(nameof(pageSize), $"Page size must be between 1 and {maxPageSize}.");

        return ModelState.IsValid ? null : CreateValidationProblemDetails();
    }

    /// <summary>
    /// Appends X-Total-Count / X-Page / X-Page-Size / X-Total-Pages pagination headers
    /// (plus X-Pagination-Capped when the page size was capped).
    /// </summary>
    protected void SetPaginationHeaders<T>(PagedResult<T> result, PaginationParameters pagination)
    {
        Response.Headers.Append("X-Total-Count",  result.TotalCount.ToString());
        Response.Headers.Append("X-Page",         result.Page.ToString());
        Response.Headers.Append("X-Page-Size",    result.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages",  result.TotalPages.ToString());

        if (pagination.WasCapped)
        {
            Response.Headers.Append("X-Pagination-Capped",       "true");
            Response.Headers.Append("X-Pagination-Applied-Max",  pagination.AppliedMaxPageSize.ToString());
        }
    }
}