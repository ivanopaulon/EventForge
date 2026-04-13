using Prym.DTOs.Common;
using Prym.DTOs.Logging;
using EventForge.Server.Services.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Receives log batches forwarded by co-located <c>EventForge.UpdateAgent</c> instances
/// and pushes them through the same <see cref="ILogIngestionService"/> pipeline used by the
/// Blazor client — so Agent logs appear alongside Server and Client entries in the
/// centralised log viewer at <c>/dashboard/logs</c> filtered as <c>[Agent:]</c> source.
///
/// Authentication: <c>X-Maintenance-Secret</c> header matched against
/// <c>UpdateHub:MaintenanceSecret</c> in configuration — the same shared secret already
/// used by the Agent for maintenance phase notifications.
/// No JWT is required so the Agent can post logs even before a user session exists.
/// </summary>
[ApiController]
[Route("api/v1/agent-logs")]
[AllowAnonymous]
[Produces("application/json")]
public class AgentLogsController(
    ILogger<AgentLogsController> logger,
    ILogIngestionService logIngestionService,
    IConfiguration configuration) : BaseApiController
{
    /// <summary>
    /// Receives a batch of Agent log entries and enqueues them for asynchronous processing.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestAgentBatch(
        [FromBody] AgentLogBatchDto batch,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized())
        {
            logger.LogWarning(
                "Agent log batch rejected — invalid or missing X-Maintenance-Secret from {Ip}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized();
        }

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (batch.Logs is null || batch.Logs.Count == 0)
            return CreateValidationProblemDetails("Batch must contain at least one log entry.");

        if (batch.Logs.Count > AgentLogBatchDto.MaxBatchSize)
            return CreateValidationProblemDetails(
                $"Batch size exceeds maximum of {AgentLogBatchDto.MaxBatchSize}.");

        try
        {
            // Map each agent entry to a ClientLogDto so the existing ingestion pipeline
            // handles it transparently.  The [Agent:...] prefix in the message makes the
            // source immediately visible in the dashboard without schema changes.
            var agentLabel   = string.IsNullOrWhiteSpace(batch.InstallationName)
                               ? "Agent"
                               : batch.InstallationName;
            var sourcePrefix = $"[Agent:{agentLabel}]";

            var clientLogs = batch.Logs.Select(e => new ClientLogDto
            {
                Level       = e.Level,
                Message     = $"{sourcePrefix} {e.Message}",
                Exception   = e.Exception,
                Timestamp   = e.Timestamp,
                Category    = e.SourceContext ?? $"Agent.{agentLabel}",
                // Leave UserId/TenantId/Page null — agent has no user session context
            }).ToList();

            var enqueuedCount = await logIngestionService.EnqueueBatchAsync(clientLogs, cancellationToken);

            if (enqueuedCount < clientLogs.Count)
                logger.LogWarning(
                    "Agent log batch from {Installation}: only {Enqueued}/{Total} entries enqueued",
                    agentLabel, enqueuedCount, clientLogs.Count);

            return Accepted();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Failed to enqueue agent log batch.", ex);
        }
    }

    private bool IsAuthorized()
    {
        var expectedSecret = configuration["UpdateHub:MaintenanceSecret"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedSecret)) return false;
        Request.Headers.TryGetValue("X-Maintenance-Secret", out var provided);
        return string.Equals(provided, expectedSecret, StringComparison.Ordinal);
    }
}
