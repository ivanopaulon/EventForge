using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of Fast Inventory Procedure business logic service.
/// Provides centralized, testable business logic for fast inventory operations.
/// </summary>
public class InventoryFastService : IInventoryFastService
{
    private readonly ILogger<InventoryFastService> _logger;

    public InventoryFastService(ILogger<InventoryFastService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public BarcodeScanResult HandleBarcodeScanned(
        string scannedCode,
        ProductDto? currentProduct,
        Guid? selectedLocationId,
        decimal? currentQuantity,
        bool fastConfirmEnabled)
    {
        _logger.LogInformation("HandleBarcodeScanned called: code={Code}, currentProduct={ProductId}, location={LocationId}, quantity={Quantity}, fastConfirm={FastConfirm}",
            scannedCode, currentProduct?.Id, selectedLocationId, currentQuantity, fastConfirmEnabled);

        // Check if scanning the same product again with location already selected
        if (currentProduct != null && selectedLocationId.HasValue && currentQuantity.HasValue)
        {
            // This is a repeated scan of the same product
            var newQuantity = currentQuantity.Value + 1;
            
            var action = fastConfirmEnabled 
                ? BarcodeScanAction.IncrementAndConfirm 
                : BarcodeScanAction.IncrementAndFocusQuantity;

            return new BarcodeScanResult
            {
                Action = action,
                NewQuantity = newQuantity,
                LogMessage = $"Repeated scan detected. Quantity incremented to {newQuantity}. FastConfirm: {fastConfirmEnabled}"
            };
        }

        // Default: lookup the product
        return new BarcodeScanResult
        {
            Action = BarcodeScanAction.LookupProduct,
            NewQuantity = currentQuantity ?? 1,
            LogMessage = "Product lookup required"
        };
    }

    /// <inheritdoc/>
    public RowOperationResult DetermineRowOperation(
        List<InventoryDocumentRowDto>? documentRows,
        Guid productId,
        Guid locationId,
        decimal quantity,
        string? notes)
    {
        _logger.LogInformation("DetermineRowOperation: product={ProductId}, location={LocationId}, quantity={Quantity}",
            productId, locationId, quantity);

        // Check if there's already a row with the same product and location
        var existingRow = documentRows?
            .FirstOrDefault(r => r.ProductId == productId && r.LocationId == locationId);

        if (existingRow != null)
        {
            // Merge: Update existing row
            var newQuantity = existingRow.Quantity + quantity;
            var combinedNotes = CombineNotes(existingRow.Notes, notes);

            _logger.LogInformation("Existing row found (ID={RowId}). Merging: old quantity={OldQty}, added={AddedQty}, new quantity={NewQty}",
                existingRow.Id, existingRow.Quantity, quantity, newQuantity);

            return new RowOperationResult
            {
                OperationType = RowOperationType.Update,
                ExistingRowId = existingRow.Id,
                NewQuantity = newQuantity,
                CombinedNotes = combinedNotes
            };
        }
        else
        {
            // Create new row
            _logger.LogInformation("No existing row found. Creating new row.");

            return new RowOperationResult
            {
                OperationType = RowOperationType.Create,
                ExistingRowId = null,
                NewQuantity = quantity,
                CombinedNotes = notes
            };
        }
    }

    /// <inheritdoc/>
    public IEnumerable<ProductDto> SearchProducts(
        string searchTerm,
        IEnumerable<ProductDto> allProducts,
        int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            _logger.LogDebug("Empty search term, returning top {MaxResults} products", maxResults);
            return allProducts.Take(maxResults);
        }

        _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);

        var results = allProducts
            .Where(p => 
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.ShortDescription) && 
                 p.ShortDescription.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Description) && 
                 p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .Take(maxResults)
            .ToList();

        _logger.LogInformation("Search returned {Count} results", results.Count);
        return results;
    }

    /// <inheritdoc/>
    public ClearedFormState ClearProductFormState()
    {
        _logger.LogDebug("Clearing product form state");

        return new ClearedFormState
        {
            ScannedBarcode = string.Empty,
            CurrentProduct = null,
            SelectedLocationId = null,
            SelectedLocation = null,
            Quantity = 1,
            Notes = string.Empty
        };
    }

    /// <inheritdoc/>
    public string? CombineNotes(string? existingNotes, string? newNotes)
    {
        if (string.IsNullOrEmpty(newNotes))
        {
            return existingNotes;
        }
        else if (string.IsNullOrEmpty(existingNotes))
        {
            return newNotes;
        }
        else
        {
            return $"{existingNotes}; {newNotes}";
        }
    }
}
