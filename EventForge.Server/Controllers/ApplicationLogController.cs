using EventForge.Server.Services.Logs;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for application log consultation (read-only) with multi-tenant support.
/// Positioned in the observability/monitoring area for system administrators.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ApplicationLogController : BaseApiController
{
    private readonly IApplicationLogService _applicationLogService;
    private readonly ITenantContext _tenantContext;

    public ApplicationLogController(IApplicationLogService applicationLogService, ITenantContext tenantContext)
    {
        _applicationLogService = applicationLogService ?? throw new ArgumentNullException(nameof(applicationLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets paginated application logs with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated application logs</returns>
    /// <response code="200">Returns the paginated application logs</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SystemLogDto>>> GetApplicationLogs(
        [FromQuery] ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access for non-super admin users
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _applicationLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving application logs.", ex);
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
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApplicationLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationLogDto>> GetApplicationLog(
        int id,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access for non-super admin users
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var log = await _applicationLogService.GetLogByIdAsync(id, cancellationToken);

            if (log == null)
            {
                return CreateNotFoundProblem($"Application log with ID {id} not found.");
            }

            return Ok(log);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the application log.", ex);
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
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("level/{level}")]
    [ProducesResponseType(typeof(IEnumerable<ApplicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ApplicationLogDto>>> GetApplicationLogsByLevel(
        string level,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return CreateValidationProblemDetails("Log level cannot be empty.");
        }

        // Validate tenant access for non-super admin users
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var logs = await _applicationLogService.GetLogsByLevelAsync(level, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving application logs by level.", ex);
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

    /// <summary>
    /// Searches system logs with advanced filtering (SuperAdmin only).
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated system log results</returns>
    [HttpPost("search")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PagedResult<SystemLogDto>>> SearchSystemLogs([FromBody] SystemLogSearchDto searchDto)
    {
        try
        {
            // Convert SystemLogSearchDto to ApplicationLogQueryParameters
            var queryParameters = new ApplicationLogQueryParameters
            {
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize,
                FromDate = searchDto.FromDate,
                ToDate = searchDto.ToDate,
                Level = searchDto.Level
                // Map other properties as available in ApplicationLogQueryParameters
            };

            var result = await _applicationLogService.GetPagedLogsAsync(queryParameters);

            // Convert ApplicationLogDto to SystemLogDto
            var systemLogs = result.Items.Select(log => new SystemLogDto
            {
                Id = Guid.NewGuid(), // ApplicationLog likely uses int ID, create a Guid for API consistency
                Timestamp = log.Timestamp,
                Level = log.Level,
                Message = log.Message,
                Category = log.Source,
                Source = log.Source,
                // Map other properties as available
                Properties = new Dictionary<string, object>()
            });

            var response = new PagedResult<SystemLogDto>
            {
                Items = systemLogs,
                TotalCount = (int)result.TotalCount,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error searching system logs", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets system log statistics (SuperAdmin only).
    /// </summary>
    /// <returns>System log statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<object>> GetSystemLogStatistics()
    {
        try
        {
            // This would need to be implemented in the ApplicationLogService
            // For now, returning a basic implementation
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var thisWeek = today.AddDays(-7);

            var todayLogs = await _applicationLogService.GetLogsInDateRangeAsync(today, DateTime.UtcNow);
            var yesterdayLogs = await _applicationLogService.GetLogsInDateRangeAsync(yesterday, today);
            var weekLogs = await _applicationLogService.GetLogsInDateRangeAsync(thisWeek, DateTime.UtcNow);

            var statistics = new
            {
                LogsToday = todayLogs.Count(),
                LogsYesterday = yesterdayLogs.Count(),
                LogsThisWeek = weekLogs.Count(),
                LogsByLevel = weekLogs.GroupBy(l => l.Level ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                LogsBySource = weekLogs.GroupBy(l => l.Source ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                ErrorsToday = todayLogs.Count(l => l.Level == "Error"),
                WarningsToday = todayLogs.Count(l => l.Level == "Warning"),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving system log statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Exports system logs (SuperAdmin only).
    /// </summary>
    /// <param name="exportDto">Export parameters</param>
    /// <returns>Export result</returns>
    [HttpPost("export")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ExportResultDto>> ExportSystemLogs([FromBody] ExportRequestDto exportDto)
    {
        try
        {
            var exportResult = await _applicationLogService.ExportSystemLogsAsync(exportDto);
            return Ok(exportResult);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error starting system logs export", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets real-time log monitoring configuration (SuperAdmin only).
    /// </summary>
    /// <returns>Current monitoring configuration</returns>
    [HttpGet("monitoring/config")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<LogMonitoringConfigDto>> GetMonitoringConfig()
    {
        try
        {
            var config = await _applicationLogService.GetMonitoringConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving monitoring configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates real-time log monitoring configuration (SuperAdmin only).
    /// </summary>
    /// <param name="configDto">Updated monitoring configuration</param>
    /// <returns>Updated configuration</returns>
    [HttpPut("monitoring/config")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<LogMonitoringConfigDto>> UpdateMonitoringConfig([FromBody] LogMonitoringConfigDto configDto)
    {
        try
        {
            var updatedConfig = await _applicationLogService.UpdateMonitoringConfigAsync(configDto);
            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error updating monitoring configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets recent critical/error logs for real-time monitoring (SuperAdmin only).
    /// </summary>
    /// <param name="limit">Maximum number of logs to return</param>
    /// <returns>Recent critical/error logs</returns>
    [HttpGet("monitoring/recent")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<SystemLogDto>>> GetRecentCriticalLogs([FromQuery] int limit = 50)
    {
        try
        {
            var queryParameters = new ApplicationLogQueryParameters
            {
                Page = 1,
                PageSize = limit,
                Level = "Error", // Would need to support multiple levels
                FromDate = DateTime.UtcNow.AddHours(-24) // Last 24 hours
            };

            var result = await _applicationLogService.GetPagedLogsAsync(queryParameters);

            var systemLogs = result.Items.Select(log => new SystemLogDto
            {
                Id = Guid.NewGuid(),
                Timestamp = log.Timestamp,
                Level = log.Level,
                Message = log.Message,
                Category = log.Source,
                Source = log.Source,
                Properties = new Dictionary<string, object>()
            });

            return Ok(systemLogs);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving recent critical logs", error = ex.Message });
        }
    }
}