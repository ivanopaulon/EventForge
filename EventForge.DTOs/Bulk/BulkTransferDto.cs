using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Bulk;

/// <summary>
/// Request DTO for bulk warehouse transfers.
/// </summary>
public class BulkTransferDto
{
    /// <summary>
    /// Source storage facility ID.
    /// </summary>
    [Required]
    public Guid SourceFacilityId { get; set; }

    /// <summary>
    /// Destination storage facility ID.
    /// </summary>
    [Required]
    public Guid DestinationFacilityId { get; set; }

    /// <summary>
    /// Optional source storage location ID.
    /// </summary>
    public Guid? SourceLocationId { get; set; }

    /// <summary>
    /// Optional destination storage location ID.
    /// </summary>
    public Guid? DestinationLocationId { get; set; }

    /// <summary>
    /// List of items to transfer.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    [MaxLength(500, ErrorMessage = "Maximum 500 items can be transferred at once.")]
    public List<BulkTransferItemDto> Items { get; set; } = new();

    /// <summary>
    /// Optional reason for the transfer.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }

    /// <summary>
    /// Transfer date.
    /// </summary>
    public DateTime? TransferDate { get; set; }
}
