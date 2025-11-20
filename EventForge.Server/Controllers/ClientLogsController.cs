using EventForge.DTOs.Logging;
using EventForge.Server.Services.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// API controller for receiving client-side logs and forwarding them to the log ingestion pipeline.
/// Allows both authenticated and anonymous clients to send logs for centralized monitoring.
/// Anonymous access is required to capture errors during login/startup and authentication failures.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class ClientLogsController : BaseApiController
{
    private readonly ILogger<ClientLogsController> _logger;
    private readonly ILogIngestionService _logIngestionService;

    public ClientLogsController(
        ILogger<ClientLogsController> logger,
        ILogIngestionService logIngestionService)
    {
        _logger = logger;
        _logIngestionService = logIngestionService;
    }

    /// <summary>
    /// Receives a single client log entry and enqueues it for asynchronous processing.
    /// </summary>
    /// <param name="clientLog">The client log entry</param>
    /// <returns>Accepted response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogClientEntry([FromBody] ClientLogDto clientLog)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var enqueued = await _logIngestionService.EnqueueAsync(clientLog);
            if (!enqueued)
            {
                _logger.LogWarning("Failed to enqueue client log entry - queue may be full");
            }

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue client log entry");
            return CreateInternalServerErrorProblem("Failed to enqueue client log entry", ex);
        }
    }

    /// <summary>
    /// Receives a batch of client log entries and enqueues them for asynchronous processing.
    /// </summary>
    /// <param name="batchRequest">The batch of client log entries</param>
    /// <returns>Accepted response</returns>
    [HttpPost("batch")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ClientLogs")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LogClientBatch([FromBody] ClientLogBatchDto batchRequest)
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
            var enqueuedCount = await _logIngestionService.EnqueueBatchAsync(batchRequest.Logs);
            if (enqueuedCount < batchRequest.Logs.Count)
            {
                _logger.LogWarning(
                    "Only {EnqueuedCount} of {TotalCount} logs were enqueued",
                    enqueuedCount, batchRequest.Logs.Count);
            }

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue client log batch");
            return CreateInternalServerErrorProblem("Failed to enqueue client log batch", ex);
        }
    }

    /// <summary>
    /// Gets the health status of the log ingestion pipeline.
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet("ingestion/health")]
    [ProducesResponseType(typeof(LogIngestionHealthDto), StatusCodes.Status200OK)]
    public IActionResult GetIngestionHealth()
    {
        try
        {
            var healthStatus = _logIngestionService.GetHealthStatus();

            var healthDto = new LogIngestionHealthDto
            {
                Status = healthStatus.Status.ToString(),
                BacklogSize = healthStatus.BacklogSize,
                DroppedCount = healthStatus.DroppedCount,
                AverageLatencyMs = healthStatus.AverageLatencyMs,
                CircuitBreakerState = healthStatus.CircuitBreakerState,
                LastProcessedAt = healthStatus.LastProcessedAt
            };

            return Ok(healthDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve ingestion health status");
            return CreateInternalServerErrorProblem("Failed to retrieve ingestion health status", ex);
        }
    }
}
