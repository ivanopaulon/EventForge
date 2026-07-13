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
    /// Gets all stock entries with pagination and filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="productId">Optional product ID filter</param>
    /// <param name="locationId">Optional location ID filter</param>
    /// <param name="lotId">Optional lot ID filter</param>
    /// <param name="lowStock">Optional low stock filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of stock entries</returns>
    /// <response code="200">Returns the paginated list of stock entries</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("stock")]
    [ProducesResponseType(typeof(PagedResult<StockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStock(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? lotId = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetStockAsync(pagination.Page, pagination.PageSize, productId, locationId, lotId, lowStock, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock entries.", ex);
        }
    }

    /// <summary>
    /// Gets a stock entry by ID.
    /// </summary>
    /// <param name="id">Stock ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stock entry details</returns>
    /// <response code="200">Returns the stock entry</response>
    /// <response code="404">If the stock entry is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("stock/{id:guid}")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStockById(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var stock = await warehouseFacade.GetStockByIdAsync(id, cancellationToken);
            return stock is not null ? Ok(stock) : NotFound();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the stock entry.", ex);
        }
    }

    /// <summary>
    /// Creates or updates a stock entry.
    /// </summary>
    /// <param name="createDto">Stock creation/update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created/updated stock entry</returns>
    /// <response code="200">Returns the created/updated stock entry</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("stock")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrUpdateStock([FromBody] CreateStockDto createDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateOrUpdateStockAsync(createDto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating/updating the stock entry.", ex);
        }
    }

    /// <summary>
    /// Creates or updates a stock entry with enhanced validation.
    /// If stockId is provided, updates existing stock (warehouse/location cannot be changed).
    /// If stockId is null/empty, creates new stock entry.
    /// </summary>
    /// <param name="dto">Create or update stock DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created/updated stock entry</returns>
    /// <response code="200">Returns the created/updated stock entry</response>
    /// <response code="400">If the request data is invalid or attempting to change warehouse/location on existing stock</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("stock/create-or-update")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrUpdateStockEnhanced([FromBody] CreateOrUpdateStockDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateOrUpdateStockAsync(dto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation when creating/updating stock - StockId: {StockId}", dto.StockId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument when creating/updating stock - StockId: {StockId}", dto.StockId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating/updating the stock entry.", ex);
        }
    }

    /// <summary>
    /// Reserves stock for a specific quantity.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="locationId">Location ID</param>
    /// <param name="quantity">Quantity to reserve</param>
    /// <param name="lotId">Optional lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("stock/reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReserveStock(
        [FromQuery] Guid productId,
        [FromQuery] Guid locationId,
        [FromQuery] decimal quantity,
        [FromQuery] Guid? lotId = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return CreateValidationProblemDetails("Quantity must be greater than zero.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.ReserveStockAsync(productId, locationId, quantity, lotId, GetCurrentUser(), cancellationToken);
            return result ? Ok() : CreateValidationProblemDetails("Insufficient stock available for reservation.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while reserving stock.", ex);
        }
    }

    /// <summary>
    /// Gets stock entries for a specific product across all locations.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock entries for the product</returns>
    [HttpGet("stock/product/{productId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStockByProductId(Guid productId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var stocks = await warehouseFacade.GetStockByProductIdAsync(productId, cancellationToken);
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock entries for the product.", ex);
        }
    }

    /// <summary>
    /// Gets stock overview with advanced filtering and aggregation options.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="search">Search term for product name or code</param>
    /// <param name="warehouseId">Optional warehouse ID filter</param>
    /// <param name="locationId">Optional location ID filter</param>
    /// <param name="lotId">Optional lot ID filter</param>
    /// <param name="lowStock">Filter for low stock items</param>
    /// <param name="criticalStock">Filter for critical stock items</param>
    /// <param name="outOfStock">Filter for out of stock items</param>
    /// <param name="inStockOnly">Show only items with stock &gt; 0</param>
    /// <param name="showAllProducts">Include all products even without stock entries</param>
    /// <param name="detailedView">Show detailed view with location breakdown</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of stock location details</returns>
    /// <response code="200">Returns the paginated list of stock overview</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("stock/overview")]
    [ProducesResponseType(typeof(PagedResult<StockLocationDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStockOverview(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] string? search = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? lotId = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] bool? criticalStock = null,
        [FromQuery] bool? outOfStock = null,
        [FromQuery] bool? inStockOnly = null,
        [FromQuery] bool? showAllProducts = null,
        [FromQuery] bool detailedView = false,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetStockOverviewAsync(
                pagination.Page, pagination.PageSize, search, warehouseId, locationId, lotId,
                lowStock, criticalStock, outOfStock, inStockOnly, showAllProducts, detailedView,
                cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock overview.", ex);
        }
    }

    /// <summary>
    /// Returns a historical stock snapshot reconstructed from movements up to the specified reference date.
    /// Quantities are computed by replaying all stock movements up to (and including) the end of the reference date.
    /// </summary>
    /// <param name="referenceDate">Reference date (format: YYYY-MM-DD)</param>
    /// <param name="search">Optional search term for product name/code</param>
    /// <param name="warehouseId">Optional warehouse ID filter</param>
    /// <param name="locationId">Optional location ID filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stock snapshot entries at the reference date</returns>
    /// <response code="200">Returns the stock snapshot</response>
    /// <response code="400">If the referenceDate is invalid or in the future</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("stock/snapshot")]
    [ProducesResponseType(typeof(IEnumerable<StockSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStockSnapshot(
        [FromQuery] DateTime referenceDate,
        [FromQuery] string? search = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
            return CreateValidationProblemDetails("referenceDate is required.");

        if (referenceDate.Date > DateTime.UtcNow.Date)
            return CreateValidationProblemDetails("referenceDate cannot be in the future.");

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetStockSnapshotAsync(
                referenceDate, search, warehouseId, locationId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the stock snapshot.", ex);
        }
    }

    /// <summary>
    /// Returns the dates and document numbers of the most recent closed inventory documents.
    /// Used to populate quick-select shortcuts in the stock snapshot dialog.
    /// </summary>
    /// <param name="count">Maximum number of records to return (default 3, max 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent inventory document dates, ordered from newest to oldest.</returns>
    /// <response code="200">Returns the list of recent inventory dates.</response>
    /// <response code="403">If the user doesn't have access to the current tenant.</response>
    [HttpGet("stock/snapshot/recent-inventory-dates")]
    [ProducesResponseType(typeof(IReadOnlyList<InventorySnapshotDateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentInventoryDates(
        [FromQuery] int count = 3,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetRecentInventoryDatesAsync(count, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving recent inventory dates.", ex);
        }
    }

    /// <summary>
    /// Returns the raw quantities from a specific closed inventory document, with purchase cost
    /// and sale price resolved at the document date using the standard pricing rules.
    /// Unlike the movement-reconstruction snapshot, no stock movements are replayed —
    /// the returned quantities are exactly what was counted in the inventory document.
    /// </summary>
    /// <param name="documentHeaderId">ID of the closed inventory document header.</param>
    /// <param name="search">Optional search term for product name/code.</param>
    /// <param name="warehouseId">Optional warehouse ID filter.</param>
    /// <param name="locationId">Optional location ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of stock snapshot entries with the inventoried quantities.</returns>
    /// <response code="200">Returns the inventoried quantities.</response>
    /// <response code="400">If the documentHeaderId is invalid.</response>
    /// <response code="403">If the user doesn't have access to the current tenant.</response>
    [HttpGet("stock/snapshot/inventory/{documentHeaderId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StockSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventoryDocumentQuantities(
        [FromRoute] Guid documentHeaderId,
        [FromQuery] string? search = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        if (documentHeaderId == Guid.Empty)
            return CreateValidationProblemDetails("documentHeaderId is required.");

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetInventoryDocumentQuantitiesAsync(
                documentHeaderId, search, warehouseId, locationId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory document quantities.", ex);
        }
    }

    /// <summary>
    /// Adjusts stock quantity for a specific stock entry.
    /// </summary>
    /// <param name="dto">Stock adjustment data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated stock entry</returns>
    /// <response code="200">Returns the updated stock entry</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the stock entry is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("stock/adjust")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.AdjustStockAsync(dto, GetCurrentUser(), cancellationToken);
            return result is not null ? Ok(result) : CreateNotFoundProblem("Stock entry not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adjusting stock.", ex);
        }
    }

    /// <summary>
    /// Gets stock movements for a specific product at a specific location, ordered by date descending.
    /// </summary>
    [HttpGet("stock/movements")]
    [ProducesResponseType(typeof(PagedResult<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStockMovementsByProductAndLocation(
        [FromQuery] Guid productId,
        [FromQuery] Guid locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        pageSize = Math.Clamp(pageSize, 1, 200);

        try
        {
            var result = await warehouseFacade.GetStockMovementsByProductAndLocationAsync(
                productId, locationId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock movements.", ex);
        }
    }

    /// <summary>
    /// Gets paged stock movements for a product across all locations, ordered by date descending.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("movements")]
    [ProducesResponseType(typeof(PagedResult<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMovementsByProduct(
        [FromQuery] Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        pageSize = Math.Clamp(pageSize, 1, 100);

        try
        {
            var result = await warehouseFacade.GetPagedMovementsAsync(productId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving movements.", ex);
        }
    }

    /// <summary>
    /// Creates a quick stock transfer from one location to another.
    /// </summary>
    [HttpPost("stock/transfer")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> QuickStockTransfer(
        [FromBody] QuickStockTransferDto request,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (request.FromLocationId == request.ToLocationId)
            return BadRequest(new { message = "Source and destination locations must be different." });

        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero." });

        try
        {
            var currentUser = GetCurrentUser();
            var movement = await warehouseFacade.QuickStockTransferAsync(request, currentUser, cancellationToken);
            return Ok(movement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while processing the stock transfer.", ex);
        }
    }

}
