namespace EventForge.Server.DTOs.Logs;

/// <summary>
/// Data Transfer Object for application log entries.
/// </summary>
public class ApplicationLogDto
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Log level (Information, Warning, Error, Debug, etc.).
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// The log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Exception details, if any.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Additional properties in JSON format.
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Logger category or source.
    /// </summary>
    public string? Logger { get; set; }

    /// <summary>
    /// Machine name where the log was generated.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Thread ID.
    /// </summary>
    public int? ThreadId { get; set; }

    /// <summary>
    /// Process ID.
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Application name or source.
    /// </summary>
    public string? Application { get; set; }

    /// <summary>
    /// Environment (Development, Production, etc.).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Correlation ID for tracking related log entries.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// User ID associated with the log entry, if applicable.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Request path, if applicable.
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Request method (GET, POST, etc.), if applicable.
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// HTTP status code, if applicable.
    /// </summary>
    public int? StatusCode { get; set; }
}