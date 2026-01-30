using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Bulk;

/// <summary>
/// Request DTO for bulk document status change.
/// </summary>
public class BulkStatusChangeDto
{
    /// <summary>
    /// List of document header IDs to change status.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one document ID is required.")]
    [MaxLength(500, ErrorMessage = "Maximum 500 documents can have their status changed at once.")]
    public List<Guid> DocumentIds { get; set; } = new();

    /// <summary>
    /// New status to apply.
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Optional reason for the status change.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
    public string? Reason { get; set; }

    /// <summary>
    /// Status change date (defaults to current date if not specified).
    /// </summary>
    public DateTime? ChangeDate { get; set; }
}
