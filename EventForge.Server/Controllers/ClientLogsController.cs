using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Common;
using System.Security.Claims;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for receiving and processing client-side logs.
/// Integrates with existing Serilog infrastructure without requiring new tables.
/// </summary>
[Route("api/[controller]")]
public class ClientLogsController : BaseApiController
{
    private readonly ILogger<ClientLogsController> _logger;
    private readonly IConfiguration _configuration;

    public ClientLogsController(ILogger<ClientLogsController> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Receives a single client log entry and logs it to the server's logging system.
    /// </summary>
    /// <param name="clientLog">Client log entry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Acknowledgment of log receipt</returns>
    /// <response code="200">Log successfully received and processed</response>
    /// <response code="400">Invalid log data</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> LogEntry([FromBody] ClientLogDto clientLog, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            await ProcessClientLogAsync(clientLog);
            return Ok(new { message = "Log received successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log");
            return CreateInternalServerErrorProblem("Failed to process client log", ex);
        }
    }

    /// <summary>
    /// Receives multiple client log entries in a batch for efficient processing.
    /// </summary>
    /// <param name="batchRequest">Batch of client log entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch processing result</returns>
    /// <response code="200">Batch successfully processed</response>
    /// <response code="400">Invalid batch data</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> LogBatch([FromBody] ClientLogBatchDto batchRequest, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (batchRequest.Logs.Count > ClientLogBatchDto.MaxBatchSize)
        {
            return BadRequest(new { message = $"Batch size exceeds maximum allowed ({ClientLogBatchDto.MaxBatchSize})" });
        }

        var results = new List<object>();
        var successCount = 0;
        var errorCount = 0;

        foreach (var clientLog in batchRequest.Logs)
        {
            try
            {
                await ProcessClientLogAsync(clientLog);
                results.Add(new { index = results.Count, status = "success" });
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process client log in batch at index {Index}", results.Count);
                results.Add(new { index = results.Count, status = "error", error = ex.Message });
                errorCount++;
            }
        }

        return Ok(new
        {
            message = "Batch processed",
            totalCount = batchRequest.Logs.Count,
            successCount,
            errorCount,
            results,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint for client logging service.
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ClientLogs",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Processes a single client log entry and integrates it with the existing logging system.
    /// </summary>
    /// <param name="clientLog">Client log to process</param>
    private async Task ProcessClientLogAsync(ClientLogDto clientLog)
    {
        // Validate log level
        if (!IsValidLogLevel(clientLog.Level))
        {
            clientLog.Level = "Information";
        }

        // Extract user information
        var userId = GetUserIdFromContext() ?? clientLog.UserId?.ToString() ?? "anonymous";
        var userName = GetUserNameFromContext() ?? "unknown";

        // Create structured log entry with custom properties
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["Source"] = "Client",
            ["UserId"] = userId,
            ["UserName"] = userName,
            ["Page"] = clientLog.Page ?? "unknown",
            ["ClientTimestamp"] = clientLog.Timestamp,
            ["CorrelationId"] = clientLog.CorrelationId ?? HttpContext.TraceIdentifier,
            ["UserAgent"] = clientLog.UserAgent ?? Request.Headers.UserAgent.ToString(),
            ["Category"] = clientLog.Category ?? "Client",
            ["RemoteIpAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ["RequestPath"] = Request.Path.Value ?? "unknown",
            ["ClientProperties"] = clientLog.Properties ?? "{}"
        }))
        {
            // Log based on level
            switch (clientLog.Level.ToLowerInvariant())
            {
                case "debug":
                    _logger.LogDebug("[CLIENT] {Message}", clientLog.Message);
                    break;
                case "information":
                    _logger.LogInformation("[CLIENT] {Message}", clientLog.Message);
                    break;
                case "warning":
                    _logger.LogWarning("[CLIENT] {Message}", clientLog.Message);
                    break;
                case "error":
                    if (!string.IsNullOrEmpty(clientLog.Exception))
                    {
                        _logger.LogError("[CLIENT] {Message} | Exception: {Exception}", clientLog.Message, clientLog.Exception);
                    }
                    else
                    {
                        _logger.LogError("[CLIENT] {Message}", clientLog.Message);
                    }
                    break;
                case "critical":
                    if (!string.IsNullOrEmpty(clientLog.Exception))
                    {
                        _logger.LogCritical("[CLIENT] {Message} | Exception: {Exception}", clientLog.Message, clientLog.Exception);
                    }
                    else
                    {
                        _logger.LogCritical("[CLIENT] {Message}", clientLog.Message);
                    }
                    break;
                default:
                    _logger.LogInformation("[CLIENT] {Message}", clientLog.Message);
                    break;
            }
        }

        await Task.CompletedTask; // For async consistency
    }

    /// <summary>
    /// Validates if the log level is supported.
    /// </summary>
    /// <param name="level">Log level to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidLogLevel(string level)
    {
        var validLevels = new[] { "Debug", "Information", "Warning", "Error", "Critical" };
        return validLevels.Contains(level, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the user ID from the current authentication context.
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    private string? GetUserIdFromContext()
    {
        return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets the user name from the current authentication context.
    /// </summary>
    /// <returns>User name if authenticated, null otherwise</returns>
    private string? GetUserNameFromContext()
    {
        return User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.Identity?.Name;
    }
}