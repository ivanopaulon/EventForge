using EventForge.Services.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for application log consultation (read-only).
/// Positioned in the observability/monitoring area.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ApplicationLogController : BaseApiController
{
    private readonly IApplicationLogService _applicationLogService;

    public ApplicationLogController(IApplicationLogService applicationLogService)
    {
        _applicationLogService = applicationLogService ?? throw new ArgumentNullException(nameof(applicationLogService));
    }

    /// <summary>
    /// Gets paginated application logs with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated application logs</returns>
    /// <response code="200">Returns the paginated application logs</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ApplicationLogDto>>> GetApplicationLogs(
        [FromQuery] ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving application logs.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific application log by ID.
    /// </summary>
    /// <param name="id">The log entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The log entry</returns>
    /// <response code="200">Returns the log entry</response>
    /// <response code="404">If the log entry is not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApplicationLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationLogDto>> GetApplicationLog(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _applicationLogService.GetLogByIdAsync(id, cancellationToken);

            if (log == null)
            {
                return NotFound(new { message = $"Application log with ID {id} not found." });
            }

            return Ok(log);
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the application log.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets application logs by log level.
    /// </summary>
    /// <param name="level">The log level to filter by (Information, Warning, Error, Debug, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs for the specified level</returns>
    /// <response code="200">Returns the application logs for the level</response>
    /// <response code="400">If the level is invalid</response>
    [HttpGet("level/{level}")]
    [ProducesResponseType(typeof(IEnumerable<ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ApplicationLogDto>>> GetApplicationLogsByLevel(
        string level,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return BadRequest(new { message = "Log level cannot be empty." });
        }

        try
        {
            var logs = await _applicationLogService.GetLogsByLevelAsync(level, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving application logs by level.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets application logs within a date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range (ISO 8601 format)</param>
    /// <param name="toDate">End date for the range (ISO 8601 format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs within the date range</returns>
    /// <response code="200">Returns the application logs within the date range</response>
    /// <response code="400">If the date parameters are invalid</response>
    [HttpGet("daterange")]
    [ProducesResponseType(typeof(IEnumerable<ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ApplicationLogDto>>> GetApplicationLogsInDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest(new { message = "From date cannot be greater than to date." });
        }

        try
        {
            var logs = await _applicationLogService.GetLogsInDateRangeAsync(fromDate, toDate, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving application logs in date range.", error = ex.Message });
        }
    }

    // Note: This controller intentionally does not include POST, PUT, DELETE methods
    // to ensure logs remain read-only via the API as per requirements.
}