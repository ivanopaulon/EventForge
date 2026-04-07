using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Bulk;

/// <summary>
/// Represents a single item in a bulk transfer operation.
/// </summary>
public class BulkTransferItemDto
{
    /// <summary>
    /// Product ID to transfer.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Optional lot ID to transfer from.
    /// </summary>
    public Guid? LotId { get; set; }

    /// <summary>
    /// Quantity to transfer.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Optional notes for this specific item transfer.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
