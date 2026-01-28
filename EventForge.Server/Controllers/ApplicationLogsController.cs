using EventForge.DTOs.Common;
using EventForge.DTOs.Logging;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for application log operations with standardized pagination.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ApplicationLogsController : BaseApiController
{
    private readonly IApplicationLogService _service;
    private readonly ILogger<ApplicationLogsController> _logger;

    public ApplicationLogsController(
        IApplicationLogService service,
        ILogger<ApplicationLogsController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all application logs with pagination (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of application logs</returns>
    /// <response code="200">Successfully retrieved application logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">User not authorized to view application logs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DTOs.Logging.ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<DTOs.Logging.ApplicationLogDto>>> GetApplicationLogs(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetApplicationLogsAsync(pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving application logs.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving application logs.", ex);
        }
    }

    /// <summary>
    /// Retrieves application logs for a specific log level
    /// </summary>
    /// <param name="level">Log level (e.g., "Information", "Warning", "Error", "Critical")</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of application logs for the specified level</returns>
    /// <response code="200">Successfully retrieved application logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">User not authorized to view application logs</response>
    [HttpGet("level/{level}")]
    [ProducesResponseType(typeof(PagedResult<DTOs.Logging.ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<DTOs.Logging.ApplicationLogDto>>> GetLogsByLevel(
        string level,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetLogsByLevelAsync(level, pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving application logs for level {Level}.", level);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving application logs for level {level}.", ex);
        }
    }

    /// <summary>
    /// Retrieves application logs within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive, defaults to current UTC time if not provided)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of application logs within the date range</returns>
    /// <response code="200">Successfully retrieved application logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters or date range</response>
    /// <response code="403">User not authorized to view application logs</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(PagedResult<DTOs.Logging.ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<DTOs.Logging.ApplicationLogDto>>> GetLogsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetLogsByDateRangeAsync(startDate, endDate, pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving application logs for date range {StartDate} to {EndDate}.", startDate, endDate);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving application logs for the specified date range.", ex);
        }
    }
}
