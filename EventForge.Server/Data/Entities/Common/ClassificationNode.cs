using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Common;


/// <summary>
/// Represents a node in the product classification hierarchy (e.g., category, family, group).
/// </summary>
public class ClassificationNode : AuditableEntity
{
    /// <summary>
    /// Node code (e.g., CAT01, FAM02).
    /// </summary>
    [MaxLength(30, ErrorMessage = "The code cannot exceed 30 characters.")]
    [Display(Name = "Code", Description = "Unique code for the node.")]
    public string? Code { get; set; }

    /// <summary>
    /// Node name.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the classification node.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Node description.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the classification node.")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of the classification node (Category, Family, Group, etc.).
    /// </summary>
    [Display(Name = "Type", Description = "Type of the classification node.")]
    public ProductClassificationType Type { get; set; } = ProductClassificationType.Category;

    /// <summary>
    /// Status of the classification node.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the classification node.")]
    public ProductClassificationNodeStatus Status { get; set; } = ProductClassificationNodeStatus.Active;

    /// <summary>
    /// Hierarchy level (root = 0).
    /// </summary>
    [Range(0, 10, ErrorMessage = "The level must be between 0 and 10.")]
    [Display(Name = "Level", Description = "Hierarchy level of the node.")]
    public int Level { get; set; } = 0;

    /// <summary>
    /// Order for sorting nodes at the same level.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "The order must be between 0 and 1000.")]
    [Display(Name = "Order", Description = "Display order among nodes at the same level.")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Parent node ID (null if root).
    /// </summary>
    [Display(Name = "Parent Node", Description = "Identifier of the parent node.")]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Parent node (null if root).
    /// </summary>
    public ClassificationNode? Parent { get; set; }

    /// <summary>
    /// Child nodes.
    /// </summary>
    [Display(Name = "Children", Description = "Child nodes of this node.")]
    public ICollection<ClassificationNode> Children { get; set; } = new List<ClassificationNode>();
}