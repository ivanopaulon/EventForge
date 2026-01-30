namespace EventForge.DTOs.Logging;

/// <summary>
/// DTO for application log entries from the LogEntry table.
/// </summary>
public class ApplicationLogDto
{
    /// <summary>
    /// Log entry identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the log was created.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Log level (e.g., Information, Warning, Error, Critical).
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Exception details (if any).
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Machine name where the log was generated.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Username associated with the log entry.
    /// </summary>
    public string? UserName { get; set; }
}
