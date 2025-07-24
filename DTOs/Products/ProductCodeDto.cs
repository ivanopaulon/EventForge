namespace EventForge.DTOs.Products;

/// <summary>
/// DTO for ProductCode output/display operations.
/// </summary>
public class ProductCodeDto
{
    /// <summary>
    /// Unique identifier for the product code.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Code type (SKU, EAN, UPC, etc.).
    /// </summary>
    public string CodeType { get; set; } = string.Empty;

    /// <summary>
    /// Code value.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Alternative description for the code.
    /// </summary>
    public string? AlternativeDescription { get; set; }

    /// <summary>
    /// Status of the product code.
    /// </summary>
    public ProductCodeStatus Status { get; set; }

    /// <summary>
    /// Date and time when the product code was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the product code.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the product code was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the product code.
    /// </summary>
    public string? ModifiedBy { get; set; }
}