using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// API controller for receiving client-side logs and forwarding them to Serilog infrastructure.
/// Allows authenticated clients to send logs for centralized monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ClientLogsController : BaseApiController
{
    private readonly ILogger<ClientLogsController> _logger;

    public ClientLogsController(ILogger<ClientLogsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receives a single client log entry and logs it via Serilog.
    /// </summary>
    /// <param name="clientLog">The client log entry</param>
    /// <returns>Accepted response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult LogClientEntry([FromBody] ClientLogDto clientLog)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            LogClientLogEntry(clientLog);
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log entry");
            return CreateInternalServerErrorProblem("Failed to process client log entry", ex);
        }
    }

    /// <summary>
    /// Receives a batch of client log entries and logs them via Serilog.
    /// </summary>
    /// <param name="batchRequest">The batch of client log entries</param>
    /// <returns>Accepted response</returns>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult LogClientBatch([FromBody] ClientLogBatchDto batchRequest)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (batchRequest.Logs == null || batchRequest.Logs.Count == 0)
        {
            return CreateValidationProblemDetails("Batch must contain at least one log entry");
        }

        if (batchRequest.Logs.Count > ClientLogBatchDto.MaxBatchSize)
        {
            return CreateValidationProblemDetails($"Batch size exceeds maximum of {ClientLogBatchDto.MaxBatchSize}");
        }

        try
        {
            foreach (var clientLog in batchRequest.Logs)
            {
                LogClientLogEntry(clientLog);
            }

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log batch");
            return CreateInternalServerErrorProblem("Failed to process client log batch", ex);
        }
    }

    /// <summary>
    /// Logs a single client log entry to Serilog with enriched properties.
    /// </summary>
    private void LogClientLogEntry(ClientLogDto clientLog)
    {
        // Create enriched log context with client-specific properties
        var properties = new Dictionary<string, object>
        {
            ["Source"] = "Client",
            ["Page"] = clientLog.Page ?? "Unknown",
            ["UserAgent"] = clientLog.UserAgent ?? "Unknown",
            ["ClientTimestamp"] = clientLog.Timestamp,
            ["CorrelationId"] = clientLog.CorrelationId ?? Guid.NewGuid().ToString(),
            ["Category"] = clientLog.Category ?? "ClientLog"
        };

        // Add UserId if available
        if (clientLog.UserId.HasValue)
        {
            properties["UserId"] = clientLog.UserId.Value;
        }

        // Add custom properties if provided
        if (!string.IsNullOrEmpty(clientLog.Properties))
        {
            properties["ClientProperties"] = clientLog.Properties;
        }

        // Add HTTP context information
        properties["RemoteIpAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        properties["RequestPath"] = HttpContext.Request.Path.ToString();

        // Get username from authenticated user
        if (User.Identity?.IsAuthenticated == true)
        {
            properties["UserName"] = User.Identity.Name ?? "Unknown";
        }

        // Log based on level with structured properties
        using (_logger.BeginScope(properties))
        {
            switch (clientLog.Level.ToUpperInvariant())
            {
                case "DEBUG":
                    _logger.LogDebug("{Message}", clientLog.Message);
                    break;

                case "INFORMATION":
                case "INFO":
                    _logger.LogInformation("{Message}", clientLog.Message);
                    break;

                case "WARNING":
                case "WARN":
                    if (!string.IsNullOrEmpty(clientLog.Exception))
                    {
                        _logger.LogWarning("{Message} | Exception: {Exception}", clientLog.Message, clientLog.Exception);
                    }
                    else
                    {
                        _logger.LogWarning("{Message}", clientLog.Message);
                    }
                    break;

                case "ERROR":
                    if (!string.IsNullOrEmpty(clientLog.Exception))
                    {
                        _logger.LogError("{Message} | Exception: {Exception}", clientLog.Message, clientLog.Exception);
                    }
                    else
                    {
                        _logger.LogError("{Message}", clientLog.Message);
                    }
                    break;

                case "CRITICAL":
                    if (!string.IsNullOrEmpty(clientLog.Exception))
                    {
                        _logger.LogCritical("{Message} | Exception: {Exception}", clientLog.Message, clientLog.Exception);
                    }
                    else
                    {
                        _logger.LogCritical("{Message}", clientLog.Message);
                    }
                    break;

                default:
                    _logger.LogInformation("{Message}", clientLog.Message);
                    break;
            }
        }
    }
}
