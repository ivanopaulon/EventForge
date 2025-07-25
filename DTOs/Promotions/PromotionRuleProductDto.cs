namespace EventForge.DTOs.Promotions;

/// <summary>
/// DTO for PromotionRuleProduct output/display operations.
/// </summary>
public class PromotionRuleProductDto
{
    /// <summary>
    /// Unique identifier for the promotion rule product.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Promotion rule ID.
    /// </summary>
    public Guid PromotionRuleId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name for display.
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Product code for display.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Date and time when the association was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the association.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the association was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the association.
    /// </summary>
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for creating a new promotion rule product association.
/// </summary>
public class CreatePromotionRuleProductDto
{
    /// <summary>
    /// Promotion rule ID.
    /// </summary>
    public Guid PromotionRuleId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public Guid ProductId { get; set; }
}