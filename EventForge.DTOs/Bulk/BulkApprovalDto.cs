using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Bulk;

/// <summary>
/// Request DTO for bulk document approval.
/// </summary>
public class BulkApprovalDto
{
    /// <summary>
    /// List of document header IDs to approve.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one document ID is required.")]
    [MaxLength(500, ErrorMessage = "Maximum 500 documents can be approved at once.")]
    public List<Guid> DocumentIds { get; set; } = new();

    /// <summary>
    /// Optional approval notes.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Approval notes cannot exceed 1000 characters.")]
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// Approval date (defaults to current date if not specified).
    /// </summary>
    public DateTime? ApprovalDate { get; set; }
}
