using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers;

/// <summary>
/// Consolidated REST API controller for warehouse management (Storage Facilities and Storage Locations).
/// Provides unified CRUD operations with multi-tenant support and standardized patterns.
/// This controller consolidates StorageFacilitiesController and StorageLocationsController
/// to reduce endpoint fragmentation and improve maintainability.
/// </summary>
[Route("api/v1/warehouse")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class WarehouseManagementController : BaseApiController
{
    // Maximum page size for bulk operations to prevent performance issues
    private const int MaxBulkOperationPageSize = 1000;

    private readonly IStorageFacilityService _storageFacilityService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ILotService _lotService;
    private readonly IStockService _stockService;
    private readonly ISerialService _serialService;
    private readonly IStockMovementService _stockMovementService;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IProductService _productService;
    private readonly IInventoryBulkSeedService _inventoryBulkSeedService;
    private readonly IInventoryDiagnosticService _inventoryDiagnosticService;
    private readonly IStockReconciliationService _stockReconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WarehouseManagementController> _logger;
    private readonly EventForgeDbContext _context;

    public WarehouseManagementController(
        IStorageFacilityService storageFacilityService,
        IStorageLocationService storageLocationService,
        ILotService lotService,
        IStockService stockService,
        ISerialService serialService,
        IStockMovementService stockMovementService,
        IDocumentHeaderService documentHeaderService,
        IProductService productService,
        IInventoryBulkSeedService inventoryBulkSeedService,
        IInventoryDiagnosticService inventoryDiagnosticService,
        IStockReconciliationService stockReconciliationService,
        ITenantContext tenantContext,
        ILogger<WarehouseManagementController> logger,
        EventForgeDbContext context)
    {
        _storageFacilityService = storageFacilityService ?? throw new ArgumentNullException(nameof(storageFacilityService));
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
        _lotService = lotService ?? throw new ArgumentNullException(nameof(lotService));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _inventoryBulkSeedService = inventoryBulkSeedService ?? throw new ArgumentNullException(nameof(inventoryBulkSeedService));
        _inventoryDiagnosticService = inventoryDiagnosticService ?? throw new ArgumentNullException(nameof(inventoryDiagnosticService));
        _stockReconciliationService = stockReconciliationService ?? throw new ArgumentNullException(nameof(stockReconciliationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Helper Methods

    // Performance estimation constants
    // NOTE: These could be made configurable through appsettings.json in future if needed
    private const double ESTIMATED_SECONDS_PER_ROW = 0.01;
    private const int LARGE_DOCUMENT_THRESHOLD = 300;
    private const int MAX_DISPLAYED_MISSING_IDS = 5;
    private const double MIN_ESTIMATED_LOAD_TIME_SECONDS = 1.0;

    /// <summary>
    /// Enriches inventory document rows with complete product and location data using optimized batch queries.
    /// Solves N+1 query problem by fetching all related data in 3 batch queries instead of N queries per row.
    /// Performance: 500 rows = 3 queries (~5 seconds) vs 1500 queries (~60+ seconds with old method).
    /// </summary>
    private async Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync(
        IEnumerable<DocumentRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        var rowsList = rows.ToList();
        if (!rowsList.Any())
        {
            return new List<InventoryDocumentRowDto>();
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting optimized enrichment for {RowCount} inventory rows", rowsList.Count);

        // BATCH 1: Fetch ALL products in a single query
        var productIds = rowsList
            .Where(r => r.ProductId.HasValue)
            .Select(r => r.ProductId!.Value)
            .Distinct()
            .ToList();

        var productsDict = new Dictionary<Guid, Product>();
        if (productIds.Any())
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            productsDict = products.ToDictionary(p => p.Id);
            _logger.LogDebug("Batch loaded {ProductCount} unique products", productsDict.Count);
        }

        // BATCH 2: Fetch ALL locations in a single query
        var locationIds = rowsList
            .Where(r => r.LocationId.HasValue)
            .Select(r => r.LocationId!.Value)
            .Distinct()
            .ToList();

        var locationsDict = new Dictionary<Guid, StorageLocation>();
        if (locationIds.Any())
        {
            var locations = await _context.StorageLocations
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            locationsDict = locations.ToDictionary(l => l.Id);
            _logger.LogDebug("Batch loaded {LocationCount} unique locations", locationsDict.Count);
        }

        // BATCH 3: Fetch ALL stocks in a single query
        // Build list of (ProductId, LocationId) pairs for stock lookup
        var stockKeys = rowsList
            .Where(r => r.ProductId.HasValue && r.LocationId.HasValue)
            .Select(r => new { ProductId = r.ProductId!.Value, LocationId = r.LocationId!.Value })
            .Distinct()
            .ToList();

        var stocksDict = new Dictionary<(Guid ProductId, Guid LocationId), Stock>();
        if (stockKeys.Any())
        {
            var stockProductIds = stockKeys.Select(k => k.ProductId).ToList();
            var stockLocationIds = stockKeys.Select(k => k.LocationId).ToList();

            var stocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => stockProductIds.Contains(s.ProductId) &&
                           stockLocationIds.Contains(s.StorageLocationId) &&
                           s.LotId == null) // Only get stock without lot for inventory
                .ToListAsync(cancellationToken);

            stocksDict = stocks.ToDictionary(s => (s.ProductId, s.StorageLocationId));
            _logger.LogDebug("Batch loaded {StockCount} stock entries", stocksDict.Count);
        }

        // Now process all rows with O(1) dictionary lookups
        var enrichedRows = new List<InventoryDocumentRowDto>(rowsList.Count);
        foreach (var row in rowsList)
        {
            var productId = row.ProductId;
            var locationId = row.LocationId;

            // Lookup product from dictionary (O(1))
            Product? product = null;
            if (productId.HasValue)
            {
                if (!productsDict.TryGetValue(productId.Value, out product))
                {
                    // Product lookup failed - log for data quality tracking
                    _logger.LogWarning("Product {ProductId} not found in batch - using row description as fallback", productId.Value);
                }
            }

            // Lookup location from dictionary (O(1))
            StorageLocation? location = null;
            string locationName = string.Empty;
            if (locationId.HasValue)
            {
                if (locationsDict.TryGetValue(locationId.Value, out location))
                {
                    locationName = location.Code ?? string.Empty;
                }
            }

            // Lookup stock from dictionary (O(1))
            decimal? previousQuantity = null;
            decimal? adjustmentQuantity = null;
            if (productId.HasValue && locationId.HasValue)
            {
                if (stocksDict.TryGetValue((productId.Value, locationId.Value), out var stock) && stock != null)
                {
                    previousQuantity = stock.Quantity;
                    adjustmentQuantity = row.Quantity - previousQuantity;
                }
            }

            enrichedRows.Add(new InventoryDocumentRowDto
            {
                Id = row.Id,
                ProductId = productId ?? Guid.Empty,
                ProductCode = row.ProductCode ?? string.Empty,
                ProductName = product?.Name ?? row.Description, // Fallback to row description
                LocationId = locationId ?? Guid.Empty,
                LocationName = locationName,
                Quantity = row.Quantity,
                PreviousQuantity = previousQuantity,
                AdjustmentQuantity = adjustmentQuantity,
                Notes = row.Notes,
                CreatedAt = row.CreatedAt,
                CreatedBy = row.CreatedBy
            });
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Completed optimized enrichment for {RowCount} rows in {ElapsedMs}ms. " +
            "Unique products: {ProductCount}, locations: {LocationCount}, stocks: {StockCount}",
            rowsList.Count, stopwatch.ElapsedMilliseconds,
            productsDict.Count, locationsDict.Count, stocksDict.Count);

        return enrichedRows;
    }

    /// <summary>
    /// DEPRECATED: Old version with N+1 query problem. Use EnrichInventoryDocumentRowsAsync instead.
    /// This method is kept for reference but should not be used.
    /// Performance issue: 500 rows = 1500 queries = 60+ seconds.
    /// </summary>
    [Obsolete("This method has N+1 query problem. Use the optimized EnrichInventoryDocumentRowsAsync instead.", error: true)]
    private async Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync_Old(
        IEnumerable<DocumentRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        var enrichedRows = new List<InventoryDocumentRowDto>();

        foreach (var row in rows)
        {
            // Use ProductId and LocationId directly from the row
            Guid? productId = row.ProductId;
            Guid? locationId = row.LocationId;
            string productName = row.Description; // Now contains clean product name
            string locationName = string.Empty;

            // Try to fetch product data for complete information
            ProductDto? product = null;
            if (productId.HasValue)
            {
                try
                {
                    product = await _productService.GetProductByIdAsync(productId.Value, cancellationToken);
                }
                catch
                {
                    // If product fetch fails, use data from row
                }
            }

            // Try to fetch location data for complete information
            if (locationId.HasValue)
            {
                try
                {
                    var location = await _storageLocationService.GetStorageLocationByIdAsync(locationId.Value, cancellationToken);
                    locationName = location?.Code ?? string.Empty;
                }
                catch
                {
                    // If location fetch fails, leave empty
                }
            }

            // Get current stock level to show adjustment info (only if we have productId and locationId)
            decimal? previousQuantity = null;
            decimal? adjustmentQuantity = null;
            if (productId.HasValue && locationId.HasValue)
            {
                try
                {
                    var stockResult = await _stockService.GetStockAsync(
                        page: 1,
                        pageSize: 1,
                        productId: productId.Value,
                        locationId: locationId.Value,
                        lotId: null,
                        cancellationToken: cancellationToken);

                    previousQuantity = stockResult.Items.FirstOrDefault()?.Quantity;
                    if (previousQuantity.HasValue)
                    {
                        adjustmentQuantity = row.Quantity - previousQuantity.Value;
                    }
                }
                catch
                {
                    // Continue without adjustment info
                }
            }

            enrichedRows.Add(new InventoryDocumentRowDto
            {
                Id = row.Id,
                ProductId = productId ?? Guid.Empty,
                ProductCode = row.ProductCode ?? string.Empty,
                ProductName = product?.Name ?? productName,
                LocationId = locationId ?? Guid.Empty,
                LocationName = locationName,
                Quantity = row.Quantity,
                PreviousQuantity = previousQuantity,
                AdjustmentQuantity = adjustmentQuantity,
                Notes = row.Notes,
                CreatedAt = row.CreatedAt,
                CreatedBy = row.CreatedBy
            });
        }

        return enrichedRows;
    }

    #endregion

    #region Storage Facilities Management

    /// <summary>
    /// Gets all storage facilities with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of storage facilities</returns>
    /// <response code="200">Returns the paginated list of storage facilities</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("facilities")]
    [ProducesResponseType(typeof(PagedResult<StorageFacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StorageFacilityDto>>> GetStorageFacilities(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageFacilityService.GetStorageFacilitiesAsync(pagination, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving storage facilities.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving storage facilities.", ex);
        }
    }

    /// <summary>
    /// Gets a storage facility by ID.
    /// </summary>
    /// <param name="id">Storage facility ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage facility information</returns>
    /// <response code="200">Returns the storage facility</response>
    /// <response code="404">If the storage facility is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("facilities/{id:guid}")]
    [ProducesResponseType(typeof(StorageFacilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorageFacilityDto>> GetStorageFacility(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var facility = await _storageFacilityService.GetStorageFacilityByIdAsync(id, cancellationToken);
            if (facility == null)
            {
                return CreateNotFoundProblem($"Storage facility with ID {id} not found.");
            }

            return Ok(facility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the storage facility.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the storage facility.", ex);
        }
    }

    /// <summary>
    /// Creates a new storage facility.
    /// </summary>
    /// <param name="createStorageFacilityDto">Storage facility creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created storage facility information</returns>
    /// <response code="201">Storage facility created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("facilities")]
    [ProducesResponseType(typeof(StorageFacilityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorageFacilityDto>> CreateStorageFacility(
        [FromBody] CreateStorageFacilityDto createStorageFacilityDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageFacilityService.CreateStorageFacilityAsync(createStorageFacilityDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetStorageFacility), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the storage facility.");
            return CreateInternalServerErrorProblem("An error occurred while creating the storage facility.", ex);
        }
    }

    #endregion

    #region Storage Locations Management

    /// <summary>
    /// Gets all storage locations with pagination and facility filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="facilityId">Optional facility ID to filter locations</param>
    /// <param name="deleted">Filter for soft-deleted items: 'false' (default), 'true', or 'all'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of storage locations</returns>
    /// <response code="200">Returns the paginated list of storage locations</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("locations")]
    [SoftDeleteFilter]
    [ProducesResponseType(typeof(PagedResult<StorageLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StorageLocationDto>>> GetStorageLocations(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? facilityId = null,
        [FromQuery] string deleted = "false",
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageLocationService.GetStorageLocationsAsync(pagination, facilityId, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving storage locations.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving storage locations.", ex);
        }
    }

    /// <summary>
    /// Gets a storage location by ID.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage location information</returns>
    /// <response code="200">Returns the storage location</response>
    /// <response code="404">If the storage location is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("locations/{id:guid}")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorageLocationDto>> GetStorageLocation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var location = await _storageLocationService.GetStorageLocationByIdAsync(id, cancellationToken);
            if (location == null)
            {
                return CreateNotFoundProblem($"Storage location with ID {id} not found.");
            }

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the storage location.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the storage location.", ex);
        }
    }

    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    /// <param name="createStorageLocationDto">Storage location creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created storage location information</returns>
    /// <response code="201">Storage location created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("locations")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorageLocationDto>> CreateStorageLocation(
        [FromBody] CreateStorageLocationDto createStorageLocationDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageLocationService.CreateStorageLocationAsync(createStorageLocationDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetStorageLocation), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the storage location.");
            return CreateInternalServerErrorProblem("An error occurred while creating the storage location.", ex);
        }
    }

    #endregion

    #region Lot Management

    /// <summary>
    /// Gets all lots with filtering and pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="productId">Optional product ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="expiringSoon">Optional filter for lots expiring soon</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of lots</returns>
    /// <response code="200">Returns the paginated list of lots</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots")]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetLots(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? expiringSoon = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.GetLotsAsync(pagination, productId, status, expiringSoon, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving lots.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving lots.", ex);
        }
    }

    /// <summary>
    /// Gets a specific lot by ID.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lot details</returns>
    /// <response code="200">Returns the lot details</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/{id:guid}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LotDto>> GetLot(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.GetLotByIdAsync(id, cancellationToken);
            return result != null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the lot.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the lot.", ex);
        }
    }

    /// <summary>
    /// Gets a specific lot by code.
    /// </summary>
    /// <param name="code">Lot code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lot details</returns>
    /// <response code="200">Returns the lot details</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/code/{code}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LotDto>> GetLotByCode(string code, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.GetLotByCodeAsync(code, cancellationToken);
            return result != null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the lot.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the lot.", ex);
        }
    }

    /// <summary>
    /// Gets lots that are expiring within the specified number of days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default: 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expiring lots</returns>
    /// <response code="200">Returns the list of expiring lots</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/expiring")]
    [ProducesResponseType(typeof(IEnumerable<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetExpiringLots(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.GetExpiringLotsAsync(daysAhead, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving expiring lots.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving expiring lots.", ex);
        }
    }

    /// <summary>
    /// Creates a new lot.
    /// </summary>
    /// <param name="createLotDto">Lot creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created lot</returns>
    /// <response code="201">Returns the newly created lot</response>
    /// <response code="400">If the lot data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If a lot with the same code already exists</response>
    [HttpPost("lots")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LotDto>> CreateLot(
        [FromBody] CreateLotDto createLotDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.CreateLotAsync(createLotDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetLot), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the lot.");
            return CreateInternalServerErrorProblem("An error occurred while creating the lot.", ex);
        }
    }

    /// <summary>
    /// Updates an existing lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="updateLotDto">Lot update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated lot</returns>
    /// <response code="200">Returns the updated lot</response>
    /// <response code="400">If the lot data is invalid</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If a lot with the same code already exists</response>
    [HttpPut("lots/{id:guid}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LotDto>> UpdateLot(
        Guid id,
        [FromBody] UpdateLotDto updateLotDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.UpdateLotAsync(id, updateLotDto, GetCurrentUser(), cancellationToken);
            return result != null ? Ok(result) : NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the lot.");
            return CreateInternalServerErrorProblem("An error occurred while updating the lot.", ex);
        }
    }

    /// <summary>
    /// Deletes a lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">If the lot was successfully deleted</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If the lot cannot be deleted due to dependencies</response>
    [HttpDelete("lots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLot(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.DeleteLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the lot.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the lot.", ex);
        }
    }

    /// <summary>
    /// Updates the quality status of a lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="qualityStatus">New quality status</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the quality status was successfully updated</response>
    /// <response code="400">If the quality status is invalid</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPatch("lots/{id:guid}/quality-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateLotQualityStatus(
        Guid id,
        [FromQuery] string qualityStatus,
        [FromQuery] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qualityStatus))
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.UpdateQualityStatusAsync(id, qualityStatus, GetCurrentUser(), notes, cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the lot quality status.");
            return CreateInternalServerErrorProblem("An error occurred while updating the lot quality status.", ex);
        }
    }

    /// <summary>
    /// Blocks a lot (prevents further operations).
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="reason">Reason for blocking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the lot was successfully blocked</response>
    /// <response code="400">If the reason is not provided</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("lots/{id:guid}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BlockLot(
        Guid id,
        [FromQuery] string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return CreateValidationProblemDetails("Reason is required for blocking a lot.");
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.BlockLotAsync(id, reason, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while blocking the lot.");
            return CreateInternalServerErrorProblem("An error occurred while blocking the lot.", ex);
        }
    }

    /// <summary>
    /// Unblocks a lot (allows further operations).
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the lot was successfully unblocked</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("lots/{id:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnblockLot(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.UnblockLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while unblocking the lot.");
            return CreateInternalServerErrorProblem("An error occurred while unblocking the lot.", ex);
        }
    }

    #endregion

    #region Stock Management

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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.GetStockAsync(pagination.Page, pagination.PageSize, productId, locationId, lotId, lowStock, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving stock entries.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stock = await _stockService.GetStockByIdAsync(id, cancellationToken);
            return stock != null ? Ok(stock) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the stock entry.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.CreateOrUpdateStockAsync(createDto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating/updating the stock entry.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.CreateOrUpdateStockAsync(dto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating/updating stock - StockId: {StockId}", dto.StockId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Operation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating/updating stock - StockId: {StockId}", dto.StockId);
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating/updating stock entry with enhanced validation.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.ReserveStockAsync(productId, locationId, quantity, lotId, GetCurrentUser(), cancellationToken);
            return result ? Ok() : BadRequest("Insufficient stock available for reservation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reserving stock.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stocks = await _stockService.GetStockByProductIdAsync(productId, cancellationToken);
            return Ok(stocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving stock entries for product {ProductId}.", productId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.GetStockOverviewAsync(
                pagination.Page, pagination.PageSize, search, warehouseId, locationId, lotId,
                lowStock, criticalStock, outOfStock, inStockOnly, showAllProducts, detailedView,
                cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving stock overview.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock overview.", ex);
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.AdjustStockAsync(dto, GetCurrentUser(), cancellationToken);
            return result != null ? Ok(result) : NotFound("Stock entry not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adjusting stock.");
            return CreateInternalServerErrorProblem("An error occurred while adjusting stock.", ex);
        }
    }

    #endregion

    #region Serial Management

    /// <summary>
    /// Gets all serials with pagination and filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="productId">Optional product ID filter</param>
    /// <param name="lotId">Optional lot ID filter</param>
    /// <param name="locationId">Optional location ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of serials</returns>
    [HttpGet("serials")]
    [ProducesResponseType(typeof(PagedResult<SerialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSerials(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? lotId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _serialService.GetSerialsAsync(pagination.Page, pagination.PageSize, productId, lotId, locationId, status, searchTerm, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving serials.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving serials.", ex);
        }
    }

    /// <summary>
    /// Gets a serial by ID.
    /// </summary>
    /// <param name="id">Serial ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serial details</returns>
    /// <response code="200">Returns the serial details</response>
    /// <response code="404">If the serial is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("serials/{id:guid}")]
    [ProducesResponseType(typeof(SerialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSerialById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var serial = await _serialService.GetSerialByIdAsync(id, cancellationToken);
            return serial != null ? Ok(serial) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the serial.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the serial.", ex);
        }
    }

    /// <summary>
    /// Creates a new serial.
    /// </summary>
    /// <param name="createDto">Serial creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created serial</returns>
    [HttpPost("serials")]
    [ProducesResponseType(typeof(SerialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSerial([FromBody] CreateSerialDto createDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _serialService.CreateSerialAsync(createDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetSerialById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the serial.");
            return CreateInternalServerErrorProblem("An error occurred while creating the serial.", ex);
        }
    }

    /// <summary>
    /// Updates a serial status.
    /// </summary>
    /// <param name="id">Serial ID</param>
    /// <param name="status">New status</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("serials/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSerialStatus(
        Guid id,
        [FromQuery] string status,
        [FromQuery] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return CreateValidationProblemDetails("Status is required.");
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _serialService.UpdateSerialStatusAsync(id, status, GetCurrentUser(), notes, cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the serial status.");
            return CreateInternalServerErrorProblem("An error occurred while updating the serial status.", ex);
        }
    }

    #endregion

    #region Inventory

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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stockResult = await _stockService.GetStockAsync(pagination.Page, pagination.PageSize, null, null, null, null, cancellationToken);

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

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving inventory entries.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get current stock level to calculate adjustment
            var existingStocks = await _stockService.GetStockAsync(
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

                _ = await _stockMovementService.ProcessAdjustmentMovementAsync(
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

            var stock = await _stockService.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);

            // Update LastInventoryDate to track when physical count was performed
            await _stockService.UpdateLastInventoryDateAsync(stock.Id, DateTime.UtcNow, cancellationToken);

            // Get location information for response
            var location = await _storageLocationService.GetStorageLocationByIdAsync(createDto.LocationId, cancellationToken);

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
            _logger.LogError(ex, "An error occurred while creating the inventory entry.");
            return CreateInternalServerErrorProblem("An error occurred while creating the inventory entry.", ex);
        }
    }

    #endregion

    #region Inventory Document Management

    /// <summary>
    /// Gets all inventory documents with pagination and filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="status">Filter by document status (Draft, Closed, etc.)</param>
    /// <param name="fromDate">Filter documents from this date</param>
    /// <param name="toDate">Filter documents to this date</param>
    /// <param name="includeRows">Whether to include document rows (default: false for performance)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of inventory documents</returns>
    /// <response code="200">Returns the paginated list of inventory documents</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents")]
    [ProducesResponseType(typeof(PagedResult<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<InventoryDocumentDto>>> GetInventoryDocuments(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            // Build query parameters to filter inventory documents
            var queryParams = new DocumentHeaderQueryParameters
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                DocumentTypeId = inventoryDocType.Id,
                IncludeRows = includeRows, // Controlled by parameter - default false for performance
                SortBy = "Date",
                SortDirection = "desc"
            };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var parsedStatus))
            {
                queryParams.Status = (EventForge.DTOs.Common.DocumentStatus)(int)parsedStatus;
            }

            if (fromDate.HasValue)
            {
                queryParams.FromDate = fromDate.Value;
            }

            if (toDate.HasValue)
            {
                queryParams.ToDate = toDate.Value;
            }

            // Get documents
            var documentsResult = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            // Convert to InventoryDocumentDto with enriched rows (only if requested)
            var inventoryDocuments = new List<InventoryDocumentDto>();
            foreach (var doc in documentsResult.Items)
            {
                // Enrich rows with complete product and location data using optimized batch method
                // Only enrich if rows were requested and are present
                var enrichedRows = includeRows && doc.Rows != null && doc.Rows.Any()
                    ? await EnrichInventoryDocumentRowsAsync(doc.Rows, cancellationToken)
                    : new List<InventoryDocumentRowDto>();

                inventoryDocuments.Add(new InventoryDocumentDto
                {
                    Id = doc.Id,
                    Number = doc.Number,
                    Series = doc.Series,
                    InventoryDate = doc.Date,
                    WarehouseId = doc.SourceWarehouseId,
                    WarehouseName = doc.SourceWarehouseName,
                    Status = doc.Status.ToString(),
                    Notes = doc.Notes,
                    CreatedAt = doc.CreatedAt,
                    CreatedBy = doc.CreatedBy,
                    FinalizedAt = doc.ClosedAt,
                    FinalizedBy = doc.Status.ToString() == "Closed" ? doc.ModifiedBy : null,
                    Rows = enrichedRows
                });
            }

            var result = new PagedResult<InventoryDocumentDto>
            {
                Items = inventoryDocuments,
                TotalCount = documentsResult.TotalCount,
                Page = documentsResult.Page,
                PageSize = documentsResult.PageSize
            };

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving inventory documents.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory documents.", ex);
        }
    }

    /// <summary>
    /// Gets an inventory document by ID.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory document</returns>
    /// <response code="200">Returns the inventory document</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/document/{documentId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Enrich rows with complete product and location data
            var enrichedRows = documentHeader.Rows != null && documentHeader.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(documentHeader.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = documentHeader.Id,
                Number = documentHeader.Number,
                Series = documentHeader.Series,
                InventoryDate = documentHeader.Date,
                WarehouseId = documentHeader.SourceWarehouseId,
                WarehouseName = documentHeader.SourceWarehouseName,
                Status = documentHeader.Status.ToString(),
                Notes = documentHeader.Notes,
                CreatedAt = documentHeader.CreatedAt,
                CreatedBy = documentHeader.CreatedBy,
                FinalizedAt = documentHeader.ClosedAt,
                FinalizedBy = documentHeader.Status.ToString() == "Closed" ? documentHeader.ModifiedBy : null,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory document.", ex);
        }
    }

    /// <summary>
    /// Starts a new inventory document.
    /// </summary>
    /// <param name="createDto">Inventory document creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created inventory document</returns>
    /// <response code="200">Returns the created inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/start")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartInventoryDocument([FromBody] CreateInventoryDocumentDto createDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return Problem("Tenant not found or invalid.", statusCode: StatusCodes.Status403Forbidden);
            }

            // Get or create an "Inventory" document type
            var inventoryDocumentType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(currentTenantId.Value, cancellationToken);

            // Get or create system business party for internal operations
            var systemBusinessPartyId = await _documentHeaderService.GetOrCreateSystemBusinessPartyAsync(currentTenantId.Value, cancellationToken);

            // Generate document number if not provided
            var documentNumber = createDto.Number ?? $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // Create a simplified document header for inventory
            var createHeaderDto = new CreateDocumentHeaderDto
            {
                DocumentTypeId = inventoryDocumentType.Id,
                Series = createDto.Series,
                Number = documentNumber,
                Date = createDto.InventoryDate,
                BusinessPartyId = systemBusinessPartyId,
                SourceWarehouseId = createDto.WarehouseId,
                Notes = createDto.Notes,
                IsFiscal = false,
                IsProforma = true
            };

            var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(createHeaderDto, GetCurrentUser(), cancellationToken);

            // Map to inventory document DTO
            var result = new InventoryDocumentDto
            {
                Id = documentHeader.Id,
                Number = documentHeader.Number,
                Series = documentHeader.Series,
                InventoryDate = documentHeader.Date,
                WarehouseId = documentHeader.SourceWarehouseId,
                WarehouseName = documentHeader.SourceWarehouseName,
                Status = documentHeader.Status.ToString(),
                Notes = documentHeader.Notes,
                CreatedAt = documentHeader.CreatedAt,
                CreatedBy = documentHeader.CreatedBy,
                Rows = new List<InventoryDocumentRowDto>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while starting the inventory document.");
            return CreateInternalServerErrorProblem("An error occurred while starting the inventory document.", ex);
        }
    }

    /// <summary>
    /// Adds a row to an existing inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowDto">Row data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/{documentId:guid}/row")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddInventoryDocumentRow(Guid documentId, [FromBody] AddInventoryDocumentRowDto rowDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Get current stock level to calculate adjustment
            var existingStocks = await _stockService.GetStockAsync(
                page: 1,
                pageSize: 1,
                productId: rowDto.ProductId,
                locationId: rowDto.LocationId,
                lotId: rowDto.LotId,
                cancellationToken: cancellationToken);

            var existingStock = existingStocks.Items.FirstOrDefault();
            var currentQuantity = existingStock?.Quantity ?? 0;
            var adjustmentQuantity = rowDto.Quantity - currentQuantity;

            // Get product and location info for the row - fetch from ProductService to ensure complete data
            var product = await _productService.GetProductByIdAsync(rowDto.ProductId, cancellationToken);
            var location = await _storageLocationService.GetStorageLocationByIdAsync(rowDto.LocationId, cancellationToken);

            if (product == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product with ID {rowDto.ProductId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (location == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Location not found",
                    Detail = $"Location with ID {rowDto.LocationId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Get unit of measure symbol if available
            string? unitOfMeasure = null;
            if (product.UnitOfMeasureId.HasValue)
            {
                try
                {
                    // Fetch unit of measure details from the database context
                    var um = await _context.UMs
                        .FirstOrDefaultAsync(u => u.Id == product.UnitOfMeasureId.Value && !u.IsDeleted, cancellationToken);
                    unitOfMeasure = um?.Symbol;
                }
                catch
                {
                    // Continue without unit of measure if fetch fails
                }
            }

            // Get VAT rate if available
            decimal vatRate = 0m;
            string? vatDescription = null;
            if (product.VatRateId.HasValue)
            {
                try
                {
                    // Fetch VAT rate details from the database context
                    var vat = await _context.VatRates
                        .FirstOrDefaultAsync(v => v.Id == product.VatRateId.Value && !v.IsDeleted, cancellationToken);
                    if (vat != null)
                    {
                        vatRate = vat.Percentage;
                        vatDescription = $"VAT {vat.Percentage}%";
                    }
                }
                catch
                {
                    // Continue without VAT rate if fetch fails
                }
            }

            // Check if a row with the same ProductId + LocationId (+ LotId if present) already exists
            // This implements the row merging feature (accorpamento delle righe per articolo/ubicazione)
            var existingRow = documentHeader.Rows?
                .FirstOrDefault(r =>
                    r.ProductId == rowDto.ProductId &&
                    r.LocationId == rowDto.LocationId);

            DocumentRowDto documentRow;

            if (existingRow != null)
            {
                // Row exists - merge by adding quantities together
                var newQuantity = existingRow.Quantity + rowDto.Quantity;

                _logger.LogInformation(
                    "Merging inventory row for product {ProductId} at location {LocationId}: existing quantity {ExistingQty} + new quantity {NewQty} = {TotalQty}",
                    rowDto.ProductId, rowDto.LocationId, existingRow.Quantity, rowDto.Quantity, newQuantity);

                // Update the existing row directly in the database
                var rowEntity = await _context.DocumentRows
                    .FirstOrDefaultAsync(r => r.Id == existingRow.Id && !r.IsDeleted, cancellationToken);

                if (rowEntity != null)
                {
                    rowEntity.Quantity = newQuantity;
                    // Append notes if new notes are provided
                    if (!string.IsNullOrWhiteSpace(rowDto.Notes))
                    {
                        rowEntity.Notes = string.IsNullOrWhiteSpace(rowEntity.Notes)
                            ? rowDto.Notes
                            : $"{rowEntity.Notes}; {rowDto.Notes}";
                    }
                    rowEntity.ModifiedAt = DateTime.UtcNow;
                    rowEntity.ModifiedBy = GetCurrentUser();

                    await _context.SaveChangesAsync(cancellationToken);

                    // Map back to DocumentRowDto
                    documentRow = new DocumentRowDto
                    {
                        Id = rowEntity.Id,
                        ProductId = rowEntity.ProductId,
                        ProductCode = rowEntity.ProductCode,
                        LocationId = rowEntity.LocationId,
                        Description = rowEntity.Description,
                        Quantity = rowEntity.Quantity,
                        Notes = rowEntity.Notes,
                        CreatedAt = rowEntity.CreatedAt,
                        CreatedBy = rowEntity.CreatedBy
                    };
                }
                else
                {
                    // Fallback if entity not found - shouldn't happen
                    throw new InvalidOperationException($"Row entity {existingRow.Id} not found in database");
                }
            }
            else
            {
                // No existing row - create a new one
                var createRowDto = new CreateDocumentRowDto
                {
                    DocumentHeaderId = documentId,
                    ProductCode = product.Code,
                    ProductId = rowDto.ProductId,
                    LocationId = rowDto.LocationId,
                    Description = product.Name, // Clean product name only
                    UnitOfMeasure = unitOfMeasure,
                    UnitOfMeasureId = rowDto.UnitOfMeasureId, // Pass UnitOfMeasureId to enable conversion
                    Quantity = rowDto.Quantity,
                    UnitPrice = 0, // Purchase price - skipped for now per requirements
                    VatRate = vatRate,
                    VatDescription = vatDescription,
                    SourceWarehouseId = location.WarehouseId, // Track the warehouse/location
                    Notes = rowDto.Notes
                };

                documentRow = await _documentHeaderService.AddDocumentRowAsync(createRowDto, GetCurrentUser(), cancellationToken);
            }

            // Build response with the new row
            var newRow = new InventoryDocumentRowDto
            {
                Id = documentRow.Id,
                ProductId = rowDto.ProductId,
                ProductName = product?.Name ?? string.Empty,
                ProductCode = product?.Code ?? string.Empty,
                LocationId = rowDto.LocationId,
                LocationName = location?.Code ?? string.Empty,
                Quantity = rowDto.Quantity,
                PreviousQuantity = currentQuantity,
                AdjustmentQuantity = adjustmentQuantity,
                LotId = rowDto.LotId,
                LotCode = existingStock?.LotCode,
                Notes = rowDto.Notes,
                CreatedAt = documentRow.CreatedAt,
                CreatedBy = documentRow.CreatedBy
            };

            // Get updated document
            var updatedDocument = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data using the helper method
            var enrichedRows = updatedDocument?.Rows != null && updatedDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument!.Id,
                Number = updatedDocument.Number,
                Series = updatedDocument.Series,
                InventoryDate = updatedDocument.Date,
                WarehouseId = updatedDocument.SourceWarehouseId,
                WarehouseName = updatedDocument.SourceWarehouseName,
                Status = updatedDocument.Status.ToString(),
                Notes = updatedDocument.Notes,
                CreatedAt = updatedDocument.CreatedAt,
                CreatedBy = updatedDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding row to inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while adding row to inventory document.", ex);
        }
    }

    /// <summary>
    /// Updates an inventory document's metadata (date, warehouse, notes).
    /// Can only update Draft documents.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="updateDto">Updated document data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid or document is not in Draft status</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("inventory/document/{documentId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInventoryDocument(Guid documentId, [FromBody] UpdateInventoryDocumentDto updateDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get the document header entity from context
            var documentHeader = await _context.DocumentHeaders
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == documentId && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Only allow updating Draft documents (status is Open in entity)
            if (documentHeader.Status != DocumentStatus.Open)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Document cannot be updated",
                    Detail = "Only Draft inventory documents can be updated. This document has already been finalized.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Update the allowed fields
            documentHeader.Date = updateDto.InventoryDate;
            documentHeader.SourceWarehouseId = updateDto.WarehouseId;
            documentHeader.Notes = updateDto.Notes;
            documentHeader.ModifiedBy = GetCurrentUser();
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated inventory document {DocumentId} - Date: {Date}, Warehouse: {WarehouseId}",
                documentId, updateDto.InventoryDate, updateDto.WarehouseId);

            // Get the updated document with full details
            var updatedDocument = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = updatedDocument!.Rows != null && updatedDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument.Id,
                Number = updatedDocument.Number,
                Series = updatedDocument.Series,
                InventoryDate = updatedDocument.Date,
                WarehouseId = updatedDocument.SourceWarehouseId,
                WarehouseName = updatedDocument.SourceWarehouseName,
                Status = updatedDocument.Status.ToString(),
                Notes = updatedDocument.Notes,
                CreatedAt = updatedDocument.CreatedAt,
                CreatedBy = updatedDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while updating inventory document.", ex);
        }
    }

    /// <summary>
    /// Updates an existing row in an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to update</param>
    /// <param name="rowDto">Updated row data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("inventory/document/{documentId:guid}/row/{rowId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInventoryDocumentRow(Guid documentId, Guid rowId, [FromBody] UpdateInventoryDocumentRowDto rowDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Open)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Document not editable",
                    Detail = "Cannot modify rows in a closed or cancelled inventory document.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Find the row in the database
            var rowEntity = await _context.DocumentRows
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (rowEntity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Row not found",
                    Detail = $"Row with ID {rowId} was not found in document {documentId}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Update the row product, quantity, location, and notes
            if (rowDto.ProductId.HasValue)
            {
                rowEntity.ProductId = rowDto.ProductId.Value;

                // Update product code and description from the new product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == rowDto.ProductId.Value && !p.IsDeleted, cancellationToken);

                if (product != null)
                {
                    rowEntity.ProductCode = product.Code;
                    rowEntity.Description = product.Name;
                }
            }

            rowEntity.Quantity = rowDto.Quantity;

            if (rowDto.LocationId.HasValue)
            {
                rowEntity.LocationId = rowDto.LocationId.Value;
            }

            rowEntity.Notes = rowDto.Notes;
            rowEntity.ModifiedAt = DateTime.UtcNow;
            rowEntity.ModifiedBy = GetCurrentUser();

            await _context.SaveChangesAsync(cancellationToken);

            // Get updated document
            var updatedDocument = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
            var enrichedRows = updatedDocument?.Rows != null && updatedDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument!.Id,
                Number = updatedDocument.Number,
                Series = updatedDocument.Series,
                InventoryDate = updatedDocument.Date,
                WarehouseId = updatedDocument.SourceWarehouseId,
                WarehouseName = updatedDocument.SourceWarehouseName,
                Status = updatedDocument.Status.ToString(),
                Notes = updatedDocument.Notes,
                CreatedAt = updatedDocument.CreatedAt,
                CreatedBy = updatedDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating row {RowId} in inventory document {DocumentId}.", rowId, documentId);
            return CreateInternalServerErrorProblem("An error occurred while updating row in inventory document.", ex);
        }
    }

    /// <summary>
    /// Deletes a row from an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("inventory/document/{documentId:guid}/row/{rowId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteInventoryDocumentRow(Guid documentId, Guid rowId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Open)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Document not editable",
                    Detail = "Cannot delete rows from a closed or cancelled inventory document.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Find the row in the database
            var rowEntity = await _context.DocumentRows
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (rowEntity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Row not found",
                    Detail = $"Row with ID {rowId} was not found in document {documentId}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Soft delete the row
            rowEntity.IsDeleted = true;
            rowEntity.DeletedAt = DateTime.UtcNow;
            rowEntity.DeletedBy = GetCurrentUser();

            await _context.SaveChangesAsync(cancellationToken);

            // Get updated document
            var updatedDocument = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
            var enrichedRows = updatedDocument?.Rows != null && updatedDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument!.Id,
                Number = updatedDocument.Number,
                Series = updatedDocument.Series,
                InventoryDate = updatedDocument.Date,
                WarehouseId = updatedDocument.SourceWarehouseId,
                WarehouseName = updatedDocument.SourceWarehouseName,
                Status = updatedDocument.Status.ToString(),
                Notes = updatedDocument.Notes,
                CreatedAt = updatedDocument.CreatedAt,
                CreatedBy = updatedDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting row {RowId} from inventory document {DocumentId}.", rowId, documentId);
            return CreateInternalServerErrorProblem("An error occurred while deleting row from inventory document.", ex);
        }
    }

    /// <summary>
    /// Finalizes an inventory document and applies all stock adjustments.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Finalized inventory document</returns>
    /// <response code="200">Returns the finalized inventory document</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/{documentId:guid}/finalize")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FinalizeInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get the document header with rows
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Validate document is in Open status
            if (documentHeader.Status != EventForge.DTOs.Common.DocumentStatus.Open)
            {
                _logger.LogWarning(
                    "Cannot finalize inventory document {DocumentId}: status is {Status}, expected Open",
                    documentId, documentHeader.Status);
                
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid document status",
                    Detail = $"Cannot finalize document: status is '{documentHeader.Status}'. Only documents in 'Open' status can be finalized.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validate document has rows
            if (documentHeader.Rows == null || !documentHeader.Rows.Any())
            {
                _logger.LogWarning(
                    "Inventory document {DocumentId} has no rows to process",
                    documentId);
                
                return BadRequest(new ProblemDetails
                {
                    Title = "Empty document",
                    Detail = "Cannot finalize an inventory document with no rows.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Start timing and initialize counters
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var totalRows = documentHeader.Rows.Count;
            var processedRows = 0;
            var skippedRows = 0;

            _logger.LogInformation(
                "Starting finalization of inventory document {DocumentId} ({DocumentNumber}). Total rows: {TotalRows}",
                documentId, documentHeader.Number, totalRows);

            // Validation: verify that all ProductId and LocationId exist before processing
            var productIds = documentHeader.Rows.Where(r => r.ProductId.HasValue).Select(r => r.ProductId!.Value).Distinct().ToList();
            var locationIds = documentHeader.Rows.Where(r => r.LocationId.HasValue).Select(r => r.LocationId!.Value).Distinct().ToList();

            var existingProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var missingProducts = productIds.Except(existingProducts).ToList();
            if (missingProducts.Any())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid inventory data",
                    Detail = $"Document contains {missingProducts.Count} non-existent product(s). Cannot finalize.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validate locations
            var existingLocations = await _context.StorageLocations
                .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var missingLocations = locationIds.Except(existingLocations).ToList();
            if (missingLocations.Any())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid inventory data",
                    Detail = $"Document contains {missingLocations.Count} non-existent location(s). Cannot finalize.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Process each row and apply stock adjustments
            if (documentHeader.Rows != null && documentHeader.Rows.Any())
            {
                _logger.LogInformation("Processing {Count} inventory rows for document {DocumentId}",
                    documentHeader.Rows.Count, documentId);

                foreach (var row in documentHeader.Rows)
                {
                    try
                    {
                        // Use ProductId and LocationId directly from the row
                        Guid productId = row.ProductId ?? Guid.Empty;
                        Guid locationId = row.LocationId ?? Guid.Empty;

                        // DocumentRowDto does not contain LotId in current DTO.
                        // Preserve compilation and behaviour by treating lot as unknown here.
                        // If lot information is required, extend DocumentRowDto to include LotId at source.
                        Guid? lotId = null;

                        // Validate we have both IDs
                        if (productId == Guid.Empty || locationId == Guid.Empty)
                        {
                            _logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                            skippedRows++;
                            continue;
                        }

                        var newQuantity = row.Quantity;

                        // Get current stock level
                        var existingStocks = await _stockService.GetStockAsync(
                            page: 1,
                            pageSize: 1,
                            productId: productId,
                            locationId: locationId,
                            lotId: lotId,
                            cancellationToken: cancellationToken);

                        var currentQuantity = existingStocks.Items.FirstOrDefault()?.Quantity ?? 0;
                        var adjustmentQuantity = newQuantity - currentQuantity;

                        // Only apply adjustment if there's a difference
                        if (adjustmentQuantity != 0)
                        {
                            // 1) Create stock adjustment movement (keeps audit trail)
                            // Use the document's InventoryDate for the movement date
                            _ = await _stockMovementService.ProcessAdjustmentMovementAsync(
                                productId: productId,
                                locationId: locationId,
                                adjustmentQuantity: adjustmentQuantity,
                                reason: "Inventory Count",
                                lotId: lotId,
                                notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                currentUser: GetCurrentUser(),
                                movementDate: documentHeader.Date,
                                cancellationToken: cancellationToken);

                            _logger.LogInformation(
                                "Applied inventory adjustment for product {ProductId} at location {LocationId}: {Adjustment} (from {Current} to {New})",
                                productId, locationId, adjustmentQuantity, currentQuantity, newQuantity);

                            processedRows++;

                            // 2) Ensure the Stocks table is updated to reflect the counted quantity
                            var createStockDto = new CreateStockDto
                            {
                                ProductId = productId,
                                StorageLocationId = locationId,
                                LotId = lotId,
                                Quantity = newQuantity,
                                Notes = $"Adjusted by inventory document {documentHeader.Number}"
                                // Other fields (ReservedQuantity, MinimumLevel etc.) can be left null or set if known
                            };

                            // This call will create or update a Stock record
                            var updatedStock = await _stockService.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);

                            // Verify stock was successfully created/updated
                            if (updatedStock != null)
                            {
                                await _stockService.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                            }
                            else
                            {
                                // If stock creation/update fails, this is a critical error - propagate it
                                throw new InvalidOperationException($"Failed to create or update stock for product {productId} at location {locationId}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No adjustment needed for product {ProductId} at location {LocationId}: quantity unchanged at {Quantity}",
                                productId, locationId, currentQuantity);
                            processedRows++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}",
                            row.Id, documentId);
                        // Continue processing other rows even if one fails
                    }
                }
            }

            // Now close the document
            var closedDocument = await _documentHeaderService.CloseDocumentAsync(documentId, GetCurrentUser(), cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = closedDocument!.Rows != null && closedDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = closedDocument.Id,
                Number = closedDocument.Number,
                Series = closedDocument.Series,
                InventoryDate = closedDocument.Date,
                WarehouseId = closedDocument.SourceWarehouseId,
                WarehouseName = closedDocument.SourceWarehouseName,
                Status = closedDocument.Status.ToString(),
                Notes = closedDocument.Notes,
                CreatedAt = closedDocument.CreatedAt,
                CreatedBy = closedDocument.CreatedBy,
                FinalizedAt = closedDocument.ClosedAt,
                FinalizedBy = closedDocument.ModifiedBy,
                Rows = enrichedRows
            };

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed finalization of inventory document {DocumentId} ({DocumentNumber}) in {ElapsedMs}ms. " +
                "Rows processed: {ProcessedRows}, Rows skipped: {SkippedRows}, Total: {TotalRows}",
                documentId, documentHeader.Number, stopwatch.ElapsedMilliseconds,
                processedRows, skippedRows, totalRows);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while finalizing inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while finalizing inventory document.", ex);
        }
    }

    /// <summary>
    /// Seeds an inventory document with rows for all active products in the tenant.
    /// Creates a test inventory document with one row per product.
    /// </summary>
    /// <param name="request">Seed request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Seed operation result</returns>
    /// <response code="200">Returns the seed operation result</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/seed-all")]
    [RequireLicenseFeature("ProductManagement")]
    [ProducesResponseType(typeof(InventorySeedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedInventoryDocument(
        [FromBody] InventorySeedRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _inventoryBulkSeedService.SeedInventoryAsync(
                request,
                GetCurrentUser(),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding inventory document.");
            return CreateInternalServerErrorProblem("An error occurred while seeding inventory document.", ex);
        }
    }

    /// <summary>
    /// Validates an inventory document to identify data quality issues and estimate load time.
    /// Performs diagnostic checks without loading all rows into memory.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with issues and statistics</returns>
    /// <response code="200">Returns the validation result</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/validate")]
    [ProducesResponseType(typeof(InventoryValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ValidateInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting validation for inventory document {DocumentId}", documentId);

            var result = new InventoryValidationResultDto
            {
                DocumentId = documentId,
                Timestamp = DateTime.UtcNow,
                IsValid = true,
                Issues = new List<InventoryValidationIssue>(),
                Stats = new InventoryStats()
            };

            // 1. Verify document exists
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // 2. Count total rows without loading them
            var totalRows = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .CountAsync(cancellationToken);

            result.TotalRows = totalRows;

            if (totalRows == 0)
            {
                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Warning",
                    Code = "EMPTY_DOCUMENT",
                    Message = "Document has no rows",
                    Details = "The inventory document contains no line items"
                });
            }

            // 3. Identify rows with null ProductId or LocationId
            var rowsWithNullData = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted &&
                           (r.ProductId == null || r.LocationId == null))
                .Select(r => new { r.Id, r.ProductId, r.LocationId })
                .ToListAsync(cancellationToken);

            foreach (var row in rowsWithNullData)
            {
                var missingFields = new List<string>();
                if (row.ProductId == null) missingFields.Add("ProductId");
                if (row.LocationId == null) missingFields.Add("LocationId");

                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Error",
                    Code = "MISSING_REQUIRED_FIELD",
                    Message = $"Row has missing required fields: {string.Join(", ", missingFields)}",
                    RowId = row.Id,
                    Details = $"This row cannot be processed without {string.Join(" and ", missingFields)}"
                });
                result.IsValid = false;
            }

            // 4. Get unique product and location IDs
            var productIds = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.ProductId != null)
                .Select(r => r.ProductId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var locationIds = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.LocationId != null)
                .Select(r => r.LocationId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            result.Stats.UniqueProducts = productIds.Count;
            result.Stats.UniqueLocations = locationIds.Count;

            // 5. Verify referenced products exist
            if (productIds.Any())
            {
                var existingProductIds = await _context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                var missingProductIds = productIds.Except(existingProductIds).ToList();
                if (missingProductIds.Any())
                {
                    result.Issues.Add(new InventoryValidationIssue
                    {
                        Severity = "Error",
                        Code = "MISSING_PRODUCTS",
                        Message = $"Document references {missingProductIds.Count} non-existent product(s)",
                        Details = $"Product IDs: {string.Join(", ", missingProductIds.Take(MAX_DISPLAYED_MISSING_IDS))}" +
                                 (missingProductIds.Count > MAX_DISPLAYED_MISSING_IDS ? $" and {missingProductIds.Count - MAX_DISPLAYED_MISSING_IDS} more" : "")
                    });
                    result.IsValid = false;
                }
            }

            // 6. Verify referenced locations exist
            if (locationIds.Any())
            {
                var existingLocationIds = await _context.StorageLocations
                    .AsNoTracking()
                    .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
                    .Select(l => l.Id)
                    .ToListAsync(cancellationToken);

                var missingLocationIds = locationIds.Except(existingLocationIds).ToList();
                if (missingLocationIds.Any())
                {
                    result.Issues.Add(new InventoryValidationIssue
                    {
                        Severity = "Error",
                        Code = "MISSING_LOCATIONS",
                        Message = $"Document references {missingLocationIds.Count} non-existent location(s)",
                        Details = $"Location IDs: {string.Join(", ", missingLocationIds.Take(MAX_DISPLAYED_MISSING_IDS))}" +
                                 (missingLocationIds.Count > MAX_DISPLAYED_MISSING_IDS ? $" and {missingLocationIds.Count - MAX_DISPLAYED_MISSING_IDS} more" : "")
                    });
                    result.IsValid = false;
                }
            }

            // 7. Estimate load time based on row count
            // Optimized method: ~0.01 seconds per row (3 batch queries regardless of size)
            // Old method would be: ~0.12 seconds per row (3 queries per row)
            result.Stats.EstimatedLoadTimeSeconds = Math.Max(MIN_ESTIMATED_LOAD_TIME_SECONDS, totalRows * ESTIMATED_SECONDS_PER_ROW);

            if (totalRows > LARGE_DOCUMENT_THRESHOLD)
            {
                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Info",
                    Code = "LARGE_DOCUMENT",
                    Message = $"Document has {totalRows} rows - this is a large inventory",
                    Details = $"Estimated load time: {result.Stats.EstimatedLoadTimeSeconds:F1} seconds with optimized queries"
                });
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed validation for document {DocumentId} in {ElapsedMs}ms. " +
                "Total rows: {TotalRows}, Issues: {IssueCount}, Valid: {IsValid}",
                documentId, stopwatch.ElapsedMilliseconds, totalRows, result.Issues.Count, result.IsValid);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while validating inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while validating inventory document.", ex);
        }
    }

    /// <summary>
    /// Gets all open inventory documents (Status == "Open").
    /// Returns documents ordered by creation date (most recent first).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of open inventory documents</returns>
    /// <response code="200">Returns the list of open inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/open")]
    [ProducesResponseType(typeof(List<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentDto>>> GetOpenInventoryDocuments(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            // Query for Open status documents
            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (EventForge.DTOs.Common.DocumentStatus)(int)DocumentStatus.Open,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = true
            };

            var documentsResult = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var inventoryDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items != null)
            {
                foreach (var doc in documentsResult.Items.OrderByDescending(d => d.CreatedAt))
                {
                    // Enrich rows with product and location data
                    var enrichedRows = doc.Rows != null && doc.Rows.Any()
                        ? await EnrichInventoryDocumentRowsAsync(doc.Rows, cancellationToken)
                        : new List<InventoryDocumentRowDto>();

                    inventoryDocuments.Add(new InventoryDocumentDto
                    {
                        Id = doc.Id,
                        Number = doc.Number,
                        Series = doc.Series,
                        InventoryDate = doc.Date,
                        WarehouseId = doc.SourceWarehouseId,
                        WarehouseName = doc.SourceWarehouseName,
                        Status = doc.Status.ToString(),
                        Notes = doc.Notes,
                        CreatedAt = doc.CreatedAt,
                        CreatedBy = doc.CreatedBy,
                        FinalizedAt = doc.ClosedAt,
                        FinalizedBy = doc.Status.ToString() == "Closed" ? doc.ModifiedBy : null,
                        Rows = enrichedRows
                    });
                }
            }

            return Ok(inventoryDocuments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving open inventory documents.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Cancels an inventory document without saving (changes status to "Cancelled").
    /// Does NOT apply stock adjustments.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the document was successfully cancelled</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Update status to Cancelled using direct database access
            var documentEntity = await _context.DocumentHeaders
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (documentEntity == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            documentEntity.Status = DocumentStatus.Cancelled;
            documentEntity.ModifiedAt = DateTime.UtcNow;
            documentEntity.ModifiedBy = GetCurrentUser();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled inventory document {DocumentId} without applying adjustments", documentId);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while cancelling inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while cancelling inventory document.", ex);
        }
    }

    /// <summary>
    /// Finalizes ALL open inventory documents by applying stock adjustments to each one.
    /// This operation is transactional - if one fails, all changes are rolled back.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of finalized inventory documents</returns>
    /// <response code="200">Returns the list of finalized inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/finalize-all")]
    [ProducesResponseType(typeof(List<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentDto>>> FinalizeAllOpenInventories(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // Get all open inventory documents
            var inventoryDocType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (EventForge.DTOs.Common.DocumentStatus)(int)DocumentStatus.Open,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = false
            };

            var documentsResult = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var finalizedDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items != null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();
                _logger.LogInformation("Finalizing {Count} open inventory documents", itemsList.Count);

                foreach (var doc in itemsList)
                {
                    // Call the existing finalize logic for each document
                    // We need to get the result as InventoryDocumentDto
                    var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(doc.Id, includeRows: true, cancellationToken);

                    if (documentHeader != null)
                    {
                        // Process each row and apply stock adjustments (reuse logic from FinalizeInventoryDocument)
                        if (documentHeader.Rows != null && documentHeader.Rows.Any())
                        {
                            foreach (var row in documentHeader.Rows)
                            {
                                try
                                {
                                    Guid productId = row.ProductId ?? Guid.Empty;
                                    Guid locationId = row.LocationId ?? Guid.Empty;
                                    Guid? lotId = null;

                                    if (productId == Guid.Empty || locationId == Guid.Empty)
                                    {
                                        _logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                                        continue;
                                    }

                                    var newQuantity = row.Quantity;
                                    var existingStocks = await _stockService.GetStockAsync(
                                        page: 1,
                                        pageSize: 1,
                                        productId: productId,
                                        locationId: locationId,
                                        lotId: lotId,
                                        cancellationToken: cancellationToken);

                                    var currentQuantity = existingStocks.Items.FirstOrDefault()?.Quantity ?? 0;
                                    var adjustmentQuantity = newQuantity - currentQuantity;

                                    if (adjustmentQuantity != 0)
                                    {
                                        _ = await _stockMovementService.ProcessAdjustmentMovementAsync(
                                            productId: productId,
                                            locationId: locationId,
                                            adjustmentQuantity: adjustmentQuantity,
                                            reason: "Inventory Count - Bulk Finalization",
                                            lotId: lotId,
                                            notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                            currentUser: GetCurrentUser(),
                                            movementDate: documentHeader.Date,
                                            cancellationToken: cancellationToken);

                                        var createStockDto = new CreateStockDto
                                        {
                                            ProductId = productId,
                                            StorageLocationId = locationId,
                                            LotId = lotId,
                                            Quantity = newQuantity,
                                            Notes = $"Adjusted by inventory document {documentHeader.Number}"
                                        };

                                        var updatedStock = await _stockService.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);
                                        if (updatedStock != null)
                                        {
                                            await _stockService.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}", row.Id, doc.Id);
                                    throw; // Re-throw to trigger transaction rollback
                                }
                            }
                        }

                        // Close the document
                        var closedDocument = await _documentHeaderService.CloseDocumentAsync(doc.Id, GetCurrentUser(), cancellationToken);

                        // Enrich rows with product and location data
                        var enrichedRows = closedDocument!.Rows != null && closedDocument.Rows.Any()
                            ? await EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
                            : new List<InventoryDocumentRowDto>();

                        finalizedDocuments.Add(new InventoryDocumentDto
                        {
                            Id = closedDocument.Id,
                            Number = closedDocument.Number,
                            Series = closedDocument.Series,
                            InventoryDate = closedDocument.Date,
                            WarehouseId = closedDocument.SourceWarehouseId,
                            WarehouseName = closedDocument.SourceWarehouseName,
                            Status = closedDocument.Status.ToString(),
                            Notes = closedDocument.Notes,
                            CreatedAt = closedDocument.CreatedAt,
                            CreatedBy = closedDocument.CreatedBy,
                            FinalizedAt = closedDocument.ClosedAt,
                            FinalizedBy = closedDocument.ModifiedBy,
                            Rows = enrichedRows
                        });
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully finalized {Count} inventory documents", finalizedDocuments.Count);

            return Ok(finalizedDocuments);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "An error occurred while finalizing all open inventory documents. Transaction rolled back.");
            return CreateInternalServerErrorProblem("An error occurred while finalizing all open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Gets paginated rows from an inventory document.
    /// Useful for loading large documents incrementally without timeouts.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of inventory document rows</returns>
    /// <response code="200">Returns the paginated rows</response>
    /// <response code="400">If pagination parameters are invalid</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/{documentId:guid}/rows")]
    [ProducesResponseType(typeof(PagedResult<InventoryDocumentRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventoryDocumentRows(
        Guid documentId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            _logger.LogInformation("Fetching page {Page} of inventory document {DocumentId} rows", pagination.Page, documentId);

            // 1. Verify document exists
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Inventory document with ID {documentId} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // 2. Count total rows
            var totalRows = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .CountAsync(cancellationToken);

            // 3. Fetch only the requested page of rows
            var skip = (pagination.Page - 1) * pagination.PageSize;
            var documentRows = await _context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .Skip(skip)
                .Take(pagination.PageSize)
                .Select(r => new DocumentRowDto
                {
                    Id = r.Id,
                    DocumentHeaderId = r.DocumentHeaderId,
                    ProductId = r.ProductId,
                    ProductCode = r.ProductCode,
                    Description = r.Description,
                    LocationId = r.LocationId,
                    Quantity = r.Quantity,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy
                })
                .ToListAsync(cancellationToken);

            // 4. Enrich using optimized batch method
            var enrichedRows = await EnrichInventoryDocumentRowsAsync(documentRows, cancellationToken);

            var result = new PagedResult<InventoryDocumentRowDto>
            {
                Items = enrichedRows,
                TotalCount = totalRows,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };

            _logger.LogInformation(
                "Returned page {Page} with {Count} rows for document {DocumentId} (total: {TotalRows})",
                pagination.Page, enrichedRows.Count, documentId, totalRows);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching inventory document rows for {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while fetching inventory document rows.", ex);
        }
    }

    /// <summary>
    /// Cancels ALL open inventory documents without saving (changes status to "Cancelled").
    /// Does NOT apply stock adjustments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of inventory documents cancelled</returns>
    /// <response code="200">Returns the count of cancelled inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/cancel-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CancelAllOpenInventories(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Get all open inventory documents
            var inventoryDocType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (EventForge.DTOs.Common.DocumentStatus)(int)DocumentStatus.Open,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = false
            };

            var documentsResult = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            int cancelledCount = 0;

            if (documentsResult?.Items != null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();
                _logger.LogInformation("Cancelling {Count} open inventory documents", itemsList.Count);

                // Fetch all document entities in a single query to avoid N+1 problem
                var documentIds = itemsList.Select(d => d.Id).ToList();
                var documentEntities = await _context.DocumentHeaders
                    .Where(d => documentIds.Contains(d.Id) && !d.IsDeleted)
                    .ToListAsync(cancellationToken);

                var currentUser = GetCurrentUser();
                var now = DateTime.UtcNow;

                foreach (var documentEntity in documentEntities)
                {
                    documentEntity.Status = DocumentStatus.Cancelled;
                    documentEntity.ModifiedAt = now;
                    documentEntity.ModifiedBy = currentUser;
                    cancelledCount++;
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully cancelled {Count} inventory documents without applying adjustments", cancelledCount);
            }

            return Ok(cancelledCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while cancelling all open inventory documents.");
            return CreateInternalServerErrorProblem("An error occurred while cancelling all open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Diagnoses an inventory document to identify data quality issues.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagnostic report with issues and statistics</returns>
    /// <response code="200">Returns the diagnostic report</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/diagnose")]
    [ProducesResponseType(typeof(InventoryDiagnosticReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DiagnoseInventoryDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var report = await _inventoryDiagnosticService.DiagnoseDocumentAsync(documentId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while diagnosing inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while diagnosing the inventory document.", ex);
        }
    }

    /// <summary>
    /// Automatically repairs an inventory document based on the specified options.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="options">Repair options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Repair result with actions performed</returns>
    /// <response code="200">Returns the repair result</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/auto-repair")]
    [ProducesResponseType(typeof(InventoryRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AutoRepairInventoryDocument(Guid documentId, [FromBody] InventoryAutoRepairOptionsDto options, CancellationToken cancellationToken)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _inventoryDiagnosticService.AutoRepairDocumentAsync(documentId, options, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while auto-repairing inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while auto-repairing the inventory document.", ex);
        }
    }

    /// <summary>
    /// Manually repairs a specific row in an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to repair</param>
    /// <param name="repairData">Repair data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success indicator</returns>
    /// <response code="200">If the row was repaired successfully</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPatch("inventory/documents/{documentId:guid}/rows/{rowId:guid}/repair")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RepairInventoryRow(Guid documentId, Guid rowId, [FromBody] InventoryRowRepairDto repairData, CancellationToken cancellationToken)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _inventoryDiagnosticService.RepairRowAsync(documentId, rowId, repairData, GetCurrentUser(), cancellationToken);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Row not found",
                    Detail = $"Row with ID {rowId} was not found in document {documentId}.",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while repairing row {RowId} in document {DocumentId}.", rowId, documentId);
            return CreateInternalServerErrorProblem("An error occurred while repairing the inventory row.", ex);
        }
    }

    /// <summary>
    /// Removes problematic rows from an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowIds">List of row IDs to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows removed</returns>
    /// <response code="200">Returns the number of rows removed</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/remove-problematic-rows")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveProblematicRows(Guid documentId, [FromBody] List<Guid> rowIds, CancellationToken cancellationToken)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var removedCount = await _inventoryDiagnosticService.RemoveProblematicRowsAsync(documentId, rowIds, GetCurrentUser(), cancellationToken);
            return Ok(removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing problematic rows from document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while removing problematic rows.", ex);
        }
    }

    /// <summary>
    /// Merges multiple open inventory documents into a single consolidated document.
    /// Groups rows by (ProductId, LocationId, LotId) and sums quantities.
    /// </summary>
    /// <param name="request">Merge request with source document IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged inventory document</returns>
    /// <response code="200">Returns the merged inventory document</response>
    /// <response code="400">If validation fails</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/merge")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MergeInventoryDocuments(
        [FromBody] MergeInventoryDocumentsDto request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // 1. Validations
            if (request.SourceDocumentIds.Count < 2)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid merge request",
                    Detail = "At least 2 documents are required to merge",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // 2. Load source documents
            var documents = await _context.DocumentHeaders
                .Include(d => d.Rows)
                .Where(d => request.SourceDocumentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            if (documents.Count != request.SourceDocumentIds.Count)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid merge request",
                    Detail = "One or more source documents not found",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // 3. Verify all documents belong to the same warehouse
            var warehouseId = documents.First().SourceWarehouseId;
            if (documents.Any(d => d.SourceWarehouseId != warehouseId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid merge request",
                    Detail = "All documents must belong to the same warehouse",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // 4. Verify all documents are in Open status
            if (documents.Any(d => d.Status != DocumentStatus.Open))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid merge request",
                    Detail = "All documents must be in Open status to be merged",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // 5. Create new destination document
            var inventoryDocType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var systemBusinessPartyId = await _documentHeaderService.GetOrCreateSystemBusinessPartyAsync(
                _tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var mergedDocNumber = $"INV-MERGED-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var sourceDocNumbers = string.Join(", ", documents.Select(d => d.Number));

            var createHeaderDto = new CreateDocumentHeaderDto
            {
                DocumentTypeId = inventoryDocType.Id,
                Series = "INV",
                Number = mergedDocNumber,
                Date = DateTime.UtcNow,
                BusinessPartyId = systemBusinessPartyId,
                SourceWarehouseId = warehouseId,
                Notes = $"Merged from: {sourceDocNumbers}. {request.Notes ?? ""}",
                IsFiscal = false,
                IsProforma = true
            };

            var mergedDocument = await _documentHeaderService.CreateDocumentHeaderAsync(
                createHeaderDto,
                GetCurrentUser(),
                cancellationToken);

            // 6. Group rows by (ProductId, LocationId) and sum quantities
            var allRows = documents.SelectMany(d => d.Rows.Where(r => !r.IsDeleted)).ToList();

            var groupedRows = allRows
                .GroupBy(r => new
                {
                    ProductId = r.ProductId ?? Guid.Empty,
                    LocationId = r.LocationId ?? Guid.Empty
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.LocationId,
                    Quantity = g.Sum(r => r.Quantity),
                    ProductCode = g.First().ProductCode,
                    Description = g.First().Description,
                    Notes = string.Join("; ", g.Where(r => !string.IsNullOrWhiteSpace(r.Notes)).Select(r => r.Notes).Distinct())
                })
                .Where(g => g.ProductId != Guid.Empty && g.LocationId != Guid.Empty)
                .ToList();

            // 7. Add merged rows to the new document
            foreach (var group in groupedRows)
            {
                var createRowDto = new CreateDocumentRowDto
                {
                    DocumentHeaderId = mergedDocument.Id,
                    ProductCode = group.ProductCode,
                    ProductId = group.ProductId,
                    LocationId = group.LocationId,
                    Description = group.Description,
                    Quantity = group.Quantity,
                    Notes = group.Notes,
                    UnitPrice = 0
                };

                await _documentHeaderService.AddDocumentRowAsync(createRowDto, GetCurrentUser(), cancellationToken);
            }

            _logger.LogInformation(
                "Merged {SourceCount} inventory documents into {MergedNumber}. " +
                "Total rows: {TotalRows}, Unique rows: {UniqueRows}, Duplicates removed: {DuplicatesRemoved}",
                documents.Count, mergedDocNumber, allRows.Count, groupedRows.Count, allRows.Count - groupedRows.Count);

            // 8. Cancel the source documents
            foreach (var doc in documents)
            {
                doc.Status = DocumentStatus.Cancelled;
                doc.Notes = $"{doc.Notes ?? ""} [Merged into {mergedDocNumber}]";
                doc.ModifiedAt = DateTime.UtcNow;
                doc.ModifiedBy = GetCurrentUser();
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 9. Load the merged document with enriched rows
            var resultDocument = await _documentHeaderService.GetDocumentHeaderByIdAsync(
                mergedDocument.Id,
                includeRows: true,
                cancellationToken);

            var enrichedRows = resultDocument!.Rows != null && resultDocument.Rows.Any()
                ? await EnrichInventoryDocumentRowsAsync(resultDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = resultDocument.Id,
                Number = resultDocument.Number,
                Series = resultDocument.Series,
                InventoryDate = resultDocument.Date,
                WarehouseId = resultDocument.SourceWarehouseId,
                WarehouseName = resultDocument.SourceWarehouseName,
                Status = resultDocument.Status.ToString(),
                Notes = resultDocument.Notes,
                CreatedAt = resultDocument.CreatedAt,
                CreatedBy = resultDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while merging inventory documents.");
            return CreateInternalServerErrorProblem("An error occurred while merging inventory documents.", ex);
        }
    }

    #endregion

    #region Stock Reconciliation

    /// <summary>
    /// Calculates stock reconciliation preview.
    /// Analyzes stock discrepancies based on documents, inventories, and manual movements.
    /// This endpoint does NOT modify data - it only calculates and returns preview.
    /// </summary>
    /// <param name="request">Reconciliation request with filters and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation result with calculated quantities and discrepancies</returns>
    [HttpPost("stock-reconciliation/calculate")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(StockReconciliationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateStockReconciliation(
        [FromBody] StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating stock reconciliation preview");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _stockReconciliationService.CalculateReconciledStockAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stock reconciliation");
            return CreateInternalServerErrorProblem("An error occurred while calculating stock reconciliation.", ex);
        }
    }

    /// <summary>
    /// Applies stock reconciliation corrections to selected items.
    /// Updates stock quantities and creates adjustment movements with full audit trail.
    /// This operation is atomic - either all updates succeed or all fail.
    /// </summary>
    /// <param name="request">Apply request with items to update and reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the apply operation</returns>
    [HttpPost("stock-reconciliation/apply")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(StockReconciliationApplyResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyStockReconciliation(
        [FromBody] StockReconciliationApplyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Applying stock reconciliation for {Count} items", request.ItemsToApply.Count);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Unknown";
            var result = await _stockReconciliationService.ApplyReconciliationAsync(request, currentUser, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying stock reconciliation");
            return CreateInternalServerErrorProblem("An error occurred while applying stock reconciliation.", ex);
        }
    }

    /// <summary>
    /// Exports stock reconciliation report as Excel file.
    /// Includes summary, details, and source movements.
    /// </summary>
    /// <param name="request">Reconciliation request with filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file</returns>
    [HttpGet("stock-reconciliation/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportStockReconciliation(
        [FromQuery] StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting stock reconciliation report");

            var fileBytes = await _stockReconciliationService.ExportReconciliationReportAsync(request, cancellationToken);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("Export generated no data or feature not yet implemented");
                return StatusCode(501, new { message = "Excel export feature not yet implemented" });
            }

            var fileName = $"StockReconciliation_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting stock reconciliation report");
            return CreateInternalServerErrorProblem("An error occurred while exporting stock reconciliation report.", ex);
        }
    }

    #endregion
}