using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for bulk seeding inventory documents with all active tenant products.
/// </summary>
public interface IInventoryBulkSeedService
{
    /// <summary>
    /// Seeds an inventory document with rows for all active products in the tenant.
    /// </summary>
    /// <param name="request">Seed request parameters</param>
    /// <param name="currentUser">Current user performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the seed operation</returns>
    Task<InventorySeedResultDto> SeedInventoryAsync(
        InventorySeedRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default);
}
