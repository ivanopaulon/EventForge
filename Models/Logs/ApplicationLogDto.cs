namespace EventForge.Models.Logs;

/// <summary>
/// DTO for Serilog application log entries.
/// </summary>
public class ApplicationLogDto
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Log message content.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Message template used for structured logging.
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Log level (Information, Warning, Error, Debug, etc.).
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Timestamp when the log entry was created (UTC).
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Exception details if an error occurred.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Additional properties in XML format.
    /// </summary>
    public string? Properties { get; set; }
}