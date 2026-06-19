using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

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
    /// Required business party type for this document (Customer, Supplier, or Both).
    /// </summary>
    [Display(Name = "Required Party Type", Description = "Required business party type for this document.")]
    public Business.BusinessPartyType RequiredPartyType { get; set; } = Business.BusinessPartyType.ClienteFornitore;

    /// <summary>
    /// Additional notes or description.
    /// </summary>
    [StringLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or description.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if this document type represents a physical inventory count.
    /// When true, the document is used for stock reconciliation calculations.
    /// </summary>
    [Display(Name = "Is Inventory Document", Description = "Indicates if this is an inventory counting document")]
    public bool IsInventoryDocument { get; set; } = false;

    /// <summary>
    /// When <c>true</c> (default), approving or closing a document of this type automatically
    /// generates warehouse stock movements.  Set to <c>false</c> for document types such as
    /// physical inventory counts whose rows represent absolute quantity anchors rather than
    /// incremental stock deltas.
    /// </summary>
    [Display(Name = "Creates Stock Movements", Description = "Whether approving or closing this document type auto-generates stock movements.")]
    public bool CreatesStockMovements { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, a stock movement is created, updated, or deleted immediately whenever
    /// a document row is added, modified, or removed — regardless of the document status.
    /// This is the "live" warehouse movement mode used, for example, by C3 documents.
    /// When enabled, <see cref="CreatesStockMovements"/> is forced to <c>false</c> because
    /// the bulk-on-archive generation would duplicate movements already created per-row.
    /// Incompatible with <see cref="IsInventoryDocument"/>.
    /// </summary>
    [Display(Name = "Moves Stock On Row Change", Description = "Whether a stock movement is generated immediately on every add/update/delete of a document row.")]
    public bool MovesStockOnRowChange { get; set; } = true;
}