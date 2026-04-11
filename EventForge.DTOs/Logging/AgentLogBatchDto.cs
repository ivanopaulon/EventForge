using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Logging;

/// <summary>
/// A single log entry forwarded from an UpdateAgent to the Server ingestion pipeline.
/// </summary>
public class AgentLogEntryDto
{
    /// <summary>UTC timestamp when the log was produced on the agent machine.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Serilog level name: Verbose, Debug, Information, Warning, Error, Fatal.</summary>
    [Required]
    [StringLength(50)]
    public string Level { get; set; } = "Information";

    /// <summary>Rendered log message (max 5 000 chars).</summary>
    [Required]
    [StringLength(5000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Exception string (if any), null otherwise.</summary>
    [StringLength(10000)]
    public string? Exception { get; set; }

    /// <summary>Serilog SourceContext (logger category name), e.g. "EventForge.UpdateAgent.Workers.AgentWorker".</summary>
    [StringLength(300)]
    public string? SourceContext { get; set; }
}

/// <summary>
/// Batch of log entries sent by an UpdateAgent to <c>POST /api/v1/agent-logs/batch</c>.
/// </summary>
public class AgentLogBatchDto
{
    /// <summary>Maximum number of entries allowed in a single batch.</summary>
    public const int MaxBatchSize = 50;

    /// <summary>
    /// The stable installation GUID assigned by the UpdateHub during enrollment.
    /// Used to correlate log entries with a specific installation in the Server dashboard.
    /// </summary>
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>Human-readable installation name (e.g. "Magazzino Nord – Server").</summary>
    [Required]
    [StringLength(200)]
    public string InstallationName { get; set; } = string.Empty;

    /// <summary>Log entries to ingest. Must contain at least 1 and at most <see cref="MaxBatchSize"/> entries.</summary>
    [Required]
    [MinLength(1)]
    public List<AgentLogEntryDto> Logs { get; set; } = [];
}
