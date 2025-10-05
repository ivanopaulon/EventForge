using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a single item (product/service) in a sale session.
/// </summary>
public class SaleItem : AuditableEntity
{
    /// <summary>
    /// Item unique identifier.
    /// </summary>
    public new Guid Id { get; set; }

    /// <summary>
    /// Reference to the sale session.
    /// </summary>
    [Required]
    public Guid SaleSessionId { get; set; }

    /// <summary>
    /// Sale session navigation property.
    /// </summary>
    public SaleSession? SaleSession { get; set; }

    /// <summary>
    /// Product identifier.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product code for display.
    /// </summary>
    [MaxLength(50)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product name/description.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Unit price at the time of sale.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Discount percentage applied to this item.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Total amount for this line (UnitPrice * Quantity - discount).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tax rate applied.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Tax amount for this line.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Notes for this item.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if this item is a service (not a product).
    /// </summary>
    public bool IsService { get; set; }

    /// <summary>
    /// Applied promotion identifier (if any).
    /// </summary>
    public Guid? PromotionId { get; set; }
}
