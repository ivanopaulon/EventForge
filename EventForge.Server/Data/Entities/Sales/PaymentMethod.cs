using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a payment method (cash, credit card, etc.).
/// Configurable from backend admin interface.
/// </summary>
public class PaymentMethod : AuditableEntity
{
    /// <summary>
    /// Payment method code (e.g., CASH, CARD, VOUCHER).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the payment method.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the payment method.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Icon identifier for UI display.
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates if this payment method requires external integration.
    /// </summary>
    public bool RequiresIntegration { get; set; }

    /// <summary>
    /// Integration configuration (JSON).
    /// </summary>
    [MaxLength(2000)]
    public string? IntegrationConfig { get; set; }

    /// <summary>
    /// Indicates if change can be given for this payment method.
    /// </summary>
    public bool AllowsChange { get; set; } = true;

    // --- Fiscal Printer Support ---

    /// <summary>
    /// Fiscal code for fiscal printer integration (1-10).
    /// </summary>
    [Range(1, 10, ErrorMessage = "Fiscal code must be between 1 and 10.")]
    [Display(Name = "Fiscal Code", Description = "Code for fiscal printer integration (1-10).")]
    public int? FiscalCode { get; set; }
}
