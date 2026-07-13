using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class WarehouseManagementController
{

    /// <summary>
    /// Gets all inventory entries with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of inventory entries</returns>
    /// <response code="200">Returns the paginated list of inventory entries</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(PagedResult<InventoryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<InventoryEntryDto>>> GetInventoryEntries(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var stockResult = await warehouseFacade.GetStockAsync(pagination.Page, pagination.PageSize, null, null, null, null, cancellationToken);

            // Convert StockDto to InventoryEntryDto
            var inventoryEntries = stockResult.Items.Select(stock => new InventoryEntryDto
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductName = stock.ProductName ?? string.Empty,
                ProductCode = stock.ProductCode ?? string.Empty,
                LocationId = stock.StorageLocationId,
                LocationName = stock.StorageLocationCode ?? string.Empty,
                Quantity = stock.AvailableQuantity,
                LotId = stock.LotId,
                LotCode = stock.LotCode,
                Notes = stock.Notes,
                CreatedAt = stock.CreatedAt,
                CreatedBy = stock.CreatedBy
            }).ToList();

            var result = new PagedResult<InventoryEntryDto>
            {
                Items = inventoryEntries,
                TotalCount = stockResult.TotalCount,
                Page = stockResult.Page,
                PageSize = stockResult.PageSize
            };

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory entries.", ex);
        }
    }

    /// <summary>
    /// Records an inventory entry for a product at a specific location.
    /// 
    /// WHAT HAPPENS WHEN YOU INSERT AN INVENTORY QUANTITY:
    /// 1. Retrieves the current stock level (if exists) for the product/location/lot combination
    /// 2. Calculates the difference between the counted quantity and the current stock
    /// 3. Creates a StockMovement document (type: Adjustment) to record the inventory adjustment
    /// 4. Updates the Stock record with the new counted quantity
    /// 5. Sets the LastInventoryDate to track when the last physical count was performed
    /// 
    /// This creates both:
    /// - A permanent StockMovement record for audit trail and traceability
    /// - An updated Stock record with the correct quantity
    /// </summary>
    /// <param name="createDto">Inventory entry data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created inventory entry information</returns>
    /// <response code="200">Returns the created inventory entry</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory")]
    [ProducesResponseType(typeof(InventoryEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInventoryEntry([FromBody] CreateInventoryEntryDto createDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get current stock level to calculate adjustment
            var existingStocks = await warehouseFacade.GetStockAsync(
                page: 1,
                pageSize: 1,
                productId: createDto.ProductId,
                locationId: createDto.LocationId,
                lotId: createDto.LotId,
                cancellationToken: cancellationToken);

            var existingStock = existingStocks.Items.FirstOrDefault();
            var currentQuantity = existingStock?.Quantity ?? 0;
            var countedQuantity = createDto.Quantity;
            var adjustmentQuantity = countedQuantity - currentQuantity;

            // Create a StockMovement document to record the inventory adjustment
            // This provides full audit trail and traceability
            if (adjustmentQuantity != 0)
            {
                var adjustmentReason = adjustmentQuantity > 0
                    ? "Inventory Count - Found Additional Stock"
                    : "Inventory Count - Stock Shortage Detected";

                _ = await warehouseFacade.ProcessAdjustmentMovementAsync(
                    productId: createDto.ProductId,
                    locationId: createDto.LocationId,
                    adjustmentQuantity: adjustmentQuantity,
                    reason: adjustmentReason,
                    lotId: createDto.LotId,
                    notes: createDto.Notes,
                    currentUser: GetCurrentUser(),
                    cancellationToken: cancellationToken);
            }

            // Update stock record with counted quantity and set LastInventoryDate
            var createStockDto = new CreateStockDto
            {
                ProductId = createDto.ProductId,
                StorageLocationId = createDto.LocationId,
                Quantity = createDto.Quantity,
                LotId = createDto.LotId,
                Notes = createDto.Notes
            };

            var stock = await warehouseFacade.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);

            // Update LastInventoryDate to track when physical count was performed
            await warehouseFacade.UpdateLastInventoryDateAsync(stock.Id, DateTime.UtcNow, cancellationToken);

            // Get location information for response
            var location = await warehouseFacade.GetStorageLocationByIdAsync(createDto.LocationId, cancellationToken);

            // Build response
            var result = new InventoryEntryDto
            {
                Id = stock.Id,
                ProductId = createDto.ProductId,
                ProductName = stock.ProductName ?? string.Empty,
                ProductCode = stock.ProductCode ?? string.Empty,
                LocationId = createDto.LocationId,
                LocationName = location?.Code ?? string.Empty,
                Quantity = createDto.Quantity,
                LotId = createDto.LotId,
                LotCode = stock.LotCode,
                Notes = createDto.Notes,
                CreatedAt = stock.CreatedAt,
                CreatedBy = stock.CreatedBy
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the inventory entry.", ex);
        }
    }

}
