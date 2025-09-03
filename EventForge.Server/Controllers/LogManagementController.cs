using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Services.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventForge.Server.Controllers;

/// <summary>
/// Unified controller for all log management operations.
/// Consolidates application logs, audit logs, and client logs with proper security controls.
/// Access restricted to SuperAdmin and Admin roles only.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class LogManagementController : BaseApiController
{
    private readonly ILogManagementService _logManagementService;
    private readonly ILogger<LogManagementController> _logger;

    public LogManagementController(
        ILogManagementService logManagementService,
        ILogger<LogManagementController> logger)
    {
        _logManagementService = logManagementService ?? throw new ArgumentNullException(nameof(logManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Application Logs

    /// <summary>
    /// Gets paginated application logs with optional filtering and sorting.
    /// Restricted to SuperAdmin and Admin roles only.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated application logs</returns>
    /// <response code="200">Returns the paginated application logs</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving logs</response>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(PagedResult<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<SystemLogDto>>> GetApplicationLogs(
        [FromQuery] ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var result = await _logManagementService.GetApplicationLogsAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application logs with parameters: {@QueryParameters}", queryParameters);
            return CreateInternalServerErrorProblem("Error retrieving application logs", ex);
        }
    }

    /// <summary>
    /// Gets a specific application log entry by ID.
    /// Restricted to SuperAdmin and Admin roles only.
    /// </summary>
    /// <param name="id">The log entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The log entry</returns>
    /// <response code="200">Returns the log entry</response>
    /// <response code="404">If the log entry is not found</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving the log</response>
    [HttpGet("logs/{id:int}")]
    [ProducesResponseType(typeof(SystemLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemLogDto>> GetApplicationLogById(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _logManagementService.GetApplicationLogByIdAsync(id, cancellationToken);
            
            if (log == null)
            {
                return CreateNotFoundProblem($"Log entry with ID {id} not found");
            }

            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application log with ID: {LogId}", id);
            return CreateInternalServerErrorProblem("Error retrieving application log", ex);
        }
    }

    /// <summary>
    /// Gets recent error logs (last 24 hours).
    /// Restricted to SuperAdmin and Admin roles only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recent error logs</returns>
    /// <response code="200">Returns recent error logs</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving logs</response>
    [HttpGet("logs/recent-errors")]
    [ProducesResponseType(typeof(IEnumerable<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SystemLogDto>>> GetRecentErrorLogs(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _logManagementService.GetRecentErrorLogsAsync(cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent error logs");
            return CreateInternalServerErrorProblem("Error retrieving recent error logs", ex);
        }
    }

    /// <summary>
    /// Gets log statistics for a specified date range.
    /// Restricted to SuperAdmin and Admin roles only.
    /// </summary>
    /// <param name="fromDate">Start date (defaults to 7 days ago)</param>
    /// <param name="toDate">End date (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Log statistics grouped by level</returns>
    /// <response code="200">Returns log statistics</response>
    /// <response code="400">If the date range is invalid</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving statistics</response>
    [HttpGet("logs/statistics")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<string, int>>> GetLogStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-7);
        var to = toDate ?? DateTime.UtcNow;

        if (from > to)
        {
            return CreateValidationProblemDetails("fromDate cannot be greater than toDate");
        }

        if ((to - from).TotalDays > 365)
        {
            return CreateValidationProblemDetails("Date range cannot exceed 365 days");
        }

        try
        {
            var statistics = await _logManagementService.GetLogStatisticsAsync(from, to, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log statistics from {FromDate} to {ToDate}", from, to);
            return CreateInternalServerErrorProblem("Error retrieving log statistics", ex);
        }
    }

    /// <summary>
    /// Gets available log levels from the system.
    /// Restricted to SuperAdmin and Admin roles only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available log levels</returns>
    /// <response code="200">Returns available log levels</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving log levels</response>
    [HttpGet("levels")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableLogLevels(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var levels = await _logManagementService.GetAvailableLogLevelsAsync(cancellationToken);
            return Ok(levels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available log levels");
            return CreateInternalServerErrorProblem("Error retrieving log levels", ex);
        }
    }

    #endregion

    #region Client Logs

    /// <summary>
    /// Processes a single client log entry.
    /// Restricted to SuperAdmin and Admin roles only for security.
    /// </summary>
    /// <param name="clientLog">Client log entry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Acknowledgment of log receipt</returns>
    /// <response code="200">Log successfully received and processed</response>
    /// <response code="400">Invalid log data</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while processing the log</response>
    [HttpPost("client-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ProcessClientLog(
        [FromBody] ClientLogDto clientLog,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var userContext = GetUserContext();
            await _logManagementService.ProcessClientLogAsync(clientLog, userContext, cancellationToken);
            
            return Ok(new 
            { 
                message = "Client log processed successfully", 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client log: {@ClientLog}", clientLog);
            return CreateInternalServerErrorProblem("Error processing client log", ex);
        }
    }

    /// <summary>
    /// Processes multiple client log entries in a batch.
    /// Restricted to SuperAdmin and Admin roles only for security.
    /// </summary>
    /// <param name="batchRequest">Batch of client log entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch processing result</returns>
    /// <response code="200">Batch successfully processed</response>
    /// <response code="400">Invalid batch data</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while processing the batch</response>
    [HttpPost("client-logs/batch")]
    [ProducesResponseType(typeof(BatchProcessingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BatchProcessingResult>> ProcessClientLogBatch(
        [FromBody] ClientLogBatchDto batchRequest,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (batchRequest.Logs.Count > ClientLogBatchDto.MaxBatchSize)
        {
            return CreateValidationProblemDetails($"Batch size cannot exceed {ClientLogBatchDto.MaxBatchSize} entries");
        }

        try
        {
            var userContext = GetUserContext();
            var result = await _logManagementService.ProcessClientLogBatchAsync(
                batchRequest.Logs, userContext, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client log batch with {Count} entries", batchRequest.Logs.Count);
            return CreateInternalServerErrorProblem("Error processing client log batch", ex);
        }
    }

    #endregion

    #region Audit Logs

    /// <summary>
    /// Gets paginated audit logs with filtering.
    /// Restricted to SuperAdmin role only.
    /// </summary>
    /// <param name="searchDto">Search and filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit logs</returns>
    /// <response code="200">Returns paginated audit logs</response>
    /// <response code="400">If the search parameters are invalid</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an error occurred while retrieving audit logs</response>
    [HttpGet("audit-logs")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(PagedResult<AuditTrailResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<AuditTrailResponseDto>>> GetAuditLogs(
        [FromQuery] AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var result = await _logManagementService.GetAuditLogsAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs: {@SearchDto}", searchDto);
            return CreateInternalServerErrorProblem("Error retrieving audit logs", ex);
        }
    }

    /// <summary>
    /// Gets audit statistics.
    /// Restricted to SuperAdmin role only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit trail statistics</returns>
    /// <response code="200">Returns audit statistics</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an error occurred while retrieving statistics</response>
    [HttpGet("audit-logs/statistics")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(AuditTrailStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuditTrailStatisticsDto>> GetAuditStatistics(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _logManagementService.GetAuditStatisticsAsync(cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            return CreateInternalServerErrorProblem("Error retrieving audit statistics", ex);
        }
    }

    #endregion

    #region Export and Monitoring

    /// <summary>
    /// Exports logs with the specified parameters.
    /// Restricted to SuperAdmin role only.
    /// </summary>
    /// <param name="exportRequest">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result information</returns>
    /// <response code="200">Export initiated successfully</response>
    /// <response code="400">If the export parameters are invalid</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an error occurred while initiating export</response>
    [HttpPost("export")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExportResultDto>> ExportLogs(
        [FromBody] ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var result = await _logManagementService.ExportLogsAsync(exportRequest, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs: {@ExportRequest}", exportRequest);
            return CreateInternalServerErrorProblem("Error exporting logs", ex);
        }
    }

    /// <summary>
    /// Gets the current log monitoring configuration.
    /// Restricted to SuperAdmin and Admin roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current monitoring configuration</returns>
    /// <response code="200">Returns monitoring configuration</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while retrieving configuration</response>
    [HttpGet("monitoring/configuration")]
    [ProducesResponseType(typeof(LogMonitoringConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LogMonitoringConfigDto>> GetMonitoringConfiguration(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _logManagementService.GetMonitoringConfigurationAsync(cancellationToken);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitoring configuration");
            return CreateInternalServerErrorProblem("Error retrieving monitoring configuration", ex);
        }
    }

    /// <summary>
    /// Updates the log monitoring configuration.
    /// Restricted to SuperAdmin role only.
    /// </summary>
    /// <param name="config">Updated monitoring configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated monitoring configuration</returns>
    /// <response code="200">Configuration updated successfully</response>
    /// <response code="400">If the configuration is invalid</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an error occurred while updating configuration</response>
    [HttpPut("monitoring/configuration")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(LogMonitoringConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LogMonitoringConfigDto>> UpdateMonitoringConfiguration(
        [FromBody] LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var updatedConfig = await _logManagementService.UpdateMonitoringConfigurationAsync(config, cancellationToken);
            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating monitoring configuration: {@Config}", config);
            return CreateInternalServerErrorProblem("Error updating monitoring configuration", ex);
        }
    }

    #endregion

    #region System Health and Maintenance

    /// <summary>
    /// Gets log system health status.
    /// Accessible to SuperAdmin and Admin roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health status</returns>
    /// <response code="200">Returns system health status</response>
    /// <response code="403">If the user doesn't have the required role</response>
    /// <response code="500">If an error occurred while checking health</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(LogSystemHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LogSystemHealthDto>> GetSystemHealth(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _logManagementService.GetSystemHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking log system health");
            return CreateInternalServerErrorProblem("Error checking system health", ex);
        }
    }

    /// <summary>
    /// Clears log management cache to ensure fresh data retrieval.
    /// Restricted to SuperAdmin role only.
    /// </summary>
    /// <returns>Cache clear result</returns>
    /// <response code="200">Cache cleared successfully</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an error occurred while clearing cache</response>
    [HttpPost("cache/clear")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ClearCache()
    {
        try
        {
            await _logManagementService.ClearCacheAsync();
            
            return Ok(new 
            { 
                message = "Log management cache cleared successfully", 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing log management cache");
            return CreateInternalServerErrorProblem("Error clearing cache", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the current user context for logging purposes.
    /// </summary>
    /// <returns>User context string</returns>
    private string? GetUserContext()
    {
        var userName = User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.Identity?.Name;
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userId))
        {
            return $"{userName} ({userId})";
        }
        
        return userName ?? userId ?? "Unknown";
    }

    #endregion
}