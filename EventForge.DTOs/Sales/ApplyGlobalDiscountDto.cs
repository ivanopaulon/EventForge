using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales;

/// <summary>
/// DTO for applying a global discount percentage to a sale session.
/// </summary>
public class ApplyGlobalDiscountDto
{
    /// <summary>
    /// Discount percentage to apply (0-100).
    /// </summary>
    [Required(ErrorMessage = "Discount percentage is required")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Optional reason for discount.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
