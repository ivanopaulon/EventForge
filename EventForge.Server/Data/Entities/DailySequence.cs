using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities;

/// <summary>
/// Represents a daily sequence counter for generating unique codes.
/// Used for automatic code generation with format YYYYMMDDNNNNNN.
/// </summary>
public class DailySequence
{
    /// <summary>
    /// The date for which this sequence applies (primary key).
    /// </summary>
    [Key]
    public DateTime Date { get; set; }

    /// <summary>
    /// The last number assigned for this date.
    /// </summary>
    [Required]
    public long LastNumber { get; set; }
}
