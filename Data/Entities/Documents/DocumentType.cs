using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Documents;


/// <summary>
/// Configurable document type (e.g., Invoice, Delivery Note, Order, etc.).
/// </summary>
public class DocumentType : AuditableEntity
{
    /// <summary>
    /// Name of the document type.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [StringLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "Name", Description = "Name of the document type.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Code of the document type (short identifier).
    /// </summary>
    [Required(ErrorMessage = "The code is required.")]
    [StringLength(10, ErrorMessage = "The code cannot exceed 10 characters.")]
    [Display(Name = "Code", Description = "Short identifier for the document type.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this document type increases (true) or decreases (false) warehouse stock.
    /// </summary>
    [Display(Name = "Stock Increase", Description = "Indicates if this document type increases warehouse stock.")]
    public bool IsStockIncrease { get; set; }

    /// <summary>
    /// Default warehouse for this document type.
    /// </summary>
    [Display(Name = "Default Warehouse", Description = "Default warehouse for this document type.")]
    public Guid? DefaultWarehouseId { get; set; }

    /// <summary>
    /// Navigation property for the default warehouse.
    /// </summary>
    [Display(Name = "Default Warehouse", Description = "Navigation property for the default warehouse.")]
    public StorageFacility? DefaultWarehouse { get; set; }

    /// <summary>
    /// Indicates if the document is fiscal.
    /// </summary>
    [Display(Name = "Is Fiscal", Description = "Indicates if the document is fiscal.")]
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Additional notes or description.
    /// </summary>
    [StringLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or description.")]
    public string? Notes { get; set; }
}