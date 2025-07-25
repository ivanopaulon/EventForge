using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Common;

/// <summary>
/// DTO for creating a new classification node.
/// </summary>
public class CreateClassificationNodeDto
{
    /// <summary>
    /// Node code.
    /// </summary>
    [MaxLength(30, ErrorMessage = "Code cannot exceed 30 characters.")]
    public string? Code { get; set; }

    /// <summary>
    /// Node name.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Node description.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of the classification node.
    /// </summary>
    public ProductClassificationType? Type { get; set; }

    /// <summary>
    /// Status of the classification node.
    /// </summary>
    public ProductClassificationNodeStatus? Status { get; set; }

    /// <summary>
    /// Hierarchy level (root = 0).
    /// </summary>
    [Range(0, 10, ErrorMessage = "Level must be between 0 and 10.")]
    public int? Level { get; set; }

    /// <summary>
    /// Order for sorting nodes at the same level.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Order must be between 0 and 1000.")]
    public int? Order { get; set; }

    /// <summary>
    /// Parent node ID (null if root).
    /// </summary>
    public Guid? ParentId { get; set; }
}