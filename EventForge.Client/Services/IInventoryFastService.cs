using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for Fast Inventory Procedure business logic.
/// Provides testable methods for barcode scanning, row merging, product search, and form state management.
/// </summary>
public interface IInventoryFastService
{
    /// <summary>
    /// Handles barcode scanning logic including repeated scan detection and quantity increment.
    /// </summary>
    /// <param name="scannedCode">The scanned barcode</param>
    /// <param name="currentProduct">Currently selected product (if any)</param>
    /// <param name="selectedLocationId">Currently selected location (if any)</param>
    /// <param name="currentQuantity">Current quantity value</param>
    /// <param name="fastConfirmEnabled">Whether fast confirm mode is enabled</param>
    /// <returns>Scan result indicating action to take</returns>
    BarcodeScanResult HandleBarcodeScanned(
        string scannedCode,
        ProductDto? currentProduct,
        Guid? selectedLocationId,
        decimal? currentQuantity,
        bool fastConfirmEnabled);

    /// <summary>
    /// Determines if a row should be merged or created, and prepares the appropriate DTO.
    /// </summary>
    /// <param name="documentRows">Current document rows</param>
    /// <param name="productId">Product ID to add</param>
    /// <param name="locationId">Location ID</param>
    /// <param name="quantity">Quantity to add</param>
    /// <param name="notes">Notes to add</param>
    /// <returns>Row operation result with appropriate DTO</returns>
    RowOperationResult DetermineRowOperation(
        List<InventoryDocumentRowDto>? documentRows,
        Guid productId,
        Guid locationId,
        decimal quantity,
        string? notes);

    /// <summary>
    /// Searches products by Name, Code, ShortDescription, and Description fields.
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="allProducts">All available products</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Filtered list of products</returns>
    IEnumerable<ProductDto> SearchProducts(
        string searchTerm,
        IEnumerable<ProductDto> allProducts,
        int maxResults = 20);

    /// <summary>
    /// Creates a cleared form state object.
    /// </summary>
    /// <returns>Cleared form state</returns>
    ClearedFormState ClearProductFormState();

    /// <summary>
    /// Combines notes from existing and new entries.
    /// </summary>
    /// <param name="existingNotes">Existing notes</param>
    /// <param name="newNotes">New notes to add</param>
    /// <returns>Combined notes string</returns>
    string? CombineNotes(string? existingNotes, string? newNotes);
}

/// <summary>
/// Result of a barcode scan operation
/// </summary>
public class BarcodeScanResult
{
    public BarcodeScanAction Action { get; set; }
    public decimal NewQuantity { get; set; }
    public string? LogMessage { get; set; }
}

/// <summary>
/// Actions to take after barcode scan
/// </summary>
public enum BarcodeScanAction
{
    /// <summary>Product needs to be looked up</summary>
    LookupProduct,
    
    /// <summary>Increment quantity and confirm immediately (fast confirm mode)</summary>
    IncrementAndConfirm,
    
    /// <summary>Increment quantity and focus quantity field</summary>
    IncrementAndFocusQuantity
}

/// <summary>
/// Result of determining whether to merge or create a row
/// </summary>
public class RowOperationResult
{
    public RowOperationType OperationType { get; set; }
    public Guid? ExistingRowId { get; set; }
    public decimal NewQuantity { get; set; }
    public string? CombinedNotes { get; set; }
}

/// <summary>
/// Type of row operation
/// </summary>
public enum RowOperationType
{
    /// <summary>Create a new row</summary>
    Create,
    
    /// <summary>Update an existing row</summary>
    Update
}

/// <summary>
/// Cleared form state
/// </summary>
public class ClearedFormState
{
    public string ScannedBarcode { get; set; } = string.Empty;
    public ProductDto? CurrentProduct { get; set; } = null;
    public Guid? SelectedLocationId { get; set; } = null;
    public StorageLocationDto? SelectedLocation { get; set; } = null;
    public decimal Quantity { get; set; } = 1;
    public string Notes { get; set; } = string.Empty;
}
