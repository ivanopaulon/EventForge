using EventForge.DTOs.Products;

namespace EventForge.Client.Shared.Components.Dialogs.UnifiedInventoryDialog;

/// <summary>
/// Represents the state of the unified inventory dialog
/// </summary>
public class InventoryDialogState
{
    /// <summary>
    /// Current view being displayed
    /// </summary>
    public InventoryDialogView CurrentView { get; set; } = InventoryDialogView.View;

    /// <summary>
    /// The product being inventoried
    /// </summary>
    public ProductDto? Product { get; set; }

    /// <summary>
    /// Draft quantity (for edit mode)
    /// </summary>
    public decimal DraftQuantity { get; set; }

    /// <summary>
    /// Draft location ID (for edit mode)
    /// </summary>
    public Guid? DraftLocationId { get; set; }

    /// <summary>
    /// Draft notes (for edit mode)
    /// </summary>
    public string DraftNotes { get; set; } = string.Empty;

    /// <summary>
    /// Conversion factor for alternative units
    /// </summary>
    public decimal ConversionFactor { get; set; } = 1m;

    /// <summary>
    /// Product unit information (for alternative units)
    /// </summary>
    public ProductUnitDto? ProductUnit { get; set; }

    /// <summary>
    /// Whether the dialog is in saving state
    /// </summary>
    public bool IsSaving { get; set; }

    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// Original quantity (for comparison)
    /// </summary>
    public decimal? OriginalQuantity { get; set; }

    /// <summary>
    /// Original location ID (for comparison)
    /// </summary>
    public Guid? OriginalLocationId { get; set; }

    /// <summary>
    /// Original notes (for comparison)
    /// </summary>
    public string? OriginalNotes { get; set; }

    /// <summary>
    /// Whether this is an edit mode (vs insert mode)
    /// </summary>
    public bool IsEditMode { get; set; }

    /// <summary>
    /// Indicates if the stock is already assigned to a warehouse/location.
    /// If TRUE: Warehouse and Location are READ-ONLY.
    /// If FALSE: Warehouse and Location are editable.
    /// </summary>
    public bool IsStockAlreadyAssigned { get; set; }

    /// <summary>
    /// ID of the existing stock (if in edit mode).
    /// </summary>
    public Guid? ExistingStockId { get; set; }

    /// <summary>
    /// Current warehouse for existing stock (READ-ONLY in edit mode).
    /// </summary>
    public Guid? ExistingWarehouseId { get; set; }

    /// <summary>
    /// Current location for existing stock (for READ-ONLY display in edit mode).
    /// </summary>
    public Guid? ExistingLocationId { get; set; }

    /// <summary>
    /// Warehouse selected for new stock (EDITABLE in insert mode).
    /// </summary>
    public Guid? DraftWarehouseId { get; set; }
}
