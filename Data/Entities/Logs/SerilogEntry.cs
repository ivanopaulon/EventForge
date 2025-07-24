using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Data.Entities.Logs;

/// <summary>
/// Entity representing a Serilog log entry in the database.
/// This matches the standard Serilog SQL Server sink table structure.
/// </summary>
[Table("Logs")]
public class SerilogEntry
{
    /// <summary>
    /// Unique identifier for the log entry (identity column).
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
    [MaxLength(128)]
    public string? Level { get; set; }

    /// <summary>
    /// Timestamp when the log entry was created.
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