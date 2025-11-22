using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for bulk operations on supplier products.
/// </summary>
public interface ISupplierProductBulkService
{
    /// <summary>
    /// Bulk updates supplier products with transaction safety.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="currentUser">Current user performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk update operation</returns>
    Task<BulkUpdateResult> BulkUpdateSupplierProductsAsync(
        Guid supplierId,
        BulkUpdateSupplierProductsRequest request,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews the changes that would be made by a bulk update without actually applying them.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of previews showing current and new values</returns>
    Task<List<SupplierProductPreview>> PreviewBulkUpdateAsync(
        Guid supplierId,
        BulkUpdateSupplierProductsRequest request,
        CancellationToken cancellationToken = default);
}
