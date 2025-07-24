namespace EventForge.DTOs.Common;

/// <summary>
/// DTO for ClassificationNode output/display operations.
/// </summary>
public class ClassificationNodeDto
{
    /// <summary>
    /// Unique identifier for the classification node.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Node code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Node name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Node description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of the classification node.
    /// </summary>
    public ProductClassificationType Type { get; set; }

    /// <summary>
    /// Status of the classification node.
    /// </summary>
    public ProductClassificationNodeStatus Status { get; set; }

    /// <summary>
    /// Hierarchy level (root = 0).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Order for sorting nodes at the same level.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Parent node ID (null if root).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Parent node name (for display purposes).
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// Date and time when the classification node was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the classification node.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the classification node was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the classification node.
    /// </summary>
    public string? ModifiedBy { get; set; }
}