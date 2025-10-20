using EventForge.DTOs.Documents;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Data;
using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityDocumentStatus = EventForge.Server.Data.Entities.Documents.DocumentStatus;

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
    private readonly IStorageFacilityService _storageFacilityService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ILotService _lotService;
    private readonly IStockService _stockService;
    private readonly ISerialService _serialService;
    private readonly IStockMovementService _stockMovementService;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IProductService _productService;
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
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Helper Methods

    /// <summary>
    /// Enriches inventory document rows with complete product and location data.
    /// Uses proper ProductId and LocationId fields instead of parsing metadata.
    /// </summary>
    private async Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync(
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
    /// Gets all storage facilities with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageFacilityService.GetStorageFacilitiesAsync(page, pageSize, cancellationToken);
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
    /// Gets all storage locations with optional pagination and facility filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? facilityId = null,
        [FromQuery] string deleted = "false",
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _storageLocationService.GetStorageLocationsAsync(page, pageSize, facilityId, cancellationToken);
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
    /// Gets all lots with optional filtering and pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? expiringSoon = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _lotService.GetLotsAsync(page, pageSize, productId, status, expiringSoon, cancellationToken);
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
    /// Gets all stock entries with optional pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? lotId = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _stockService.GetStockAsync(page, pageSize, productId, locationId, lotId, lowStock, cancellationToken);
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

    #endregion

    #region Serial Management

    /// <summary>
    /// Gets all serials with optional pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? lotId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _serialService.GetSerialsAsync(page, pageSize, productId, lotId, locationId, status, searchTerm, cancellationToken);
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
    /// Gets all inventory entries with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stockResult = await _stockService.GetStockAsync(page, pageSize, null, null, null, null, cancellationToken);

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
    /// Gets all inventory documents with optional pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="status">Filter by document status (Draft, Closed, etc.)</param>
    /// <param name="fromDate">Filter documents from this date</param>
    /// <param name="toDate">Filter documents to this date</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

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
                Page = page,
                PageSize = pageSize,
                DocumentTypeId = inventoryDocType.Id,
                IncludeRows = true,
                SortBy = "Date",
                SortDirection = "desc"
            };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EntityDocumentStatus>(status, true, out var parsedStatus))
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

            // Convert to InventoryDocumentDto with enriched rows
            var inventoryDocuments = new List<InventoryDocumentDto>();
            foreach (var doc in documentsResult.Items)
            {
                // Enrich rows with complete product and location data
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

            var result = new PagedResult<InventoryDocumentDto>
            {
                Items = inventoryDocuments,
                TotalCount = documentsResult.TotalCount,
                Page = documentsResult.Page,
                PageSize = documentsResult.PageSize
            };

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

            // Create document row with clean description and proper field population
            var createRowDto = new CreateDocumentRowDto
            {
                DocumentHeaderId = documentId,
                ProductCode = product.Code,
                ProductId = rowDto.ProductId,
                LocationId = rowDto.LocationId,
                Description = product.Name, // Clean product name only
                UnitOfMeasure = unitOfMeasure,
                Quantity = (int)rowDto.Quantity,
                UnitPrice = 0, // Purchase price - skipped for now per requirements
                VatRate = vatRate,
                VatDescription = vatDescription,
                SourceWarehouseId = location.WarehouseId, // Track the warehouse/location
                Notes = rowDto.Notes
            };

            var documentRow = await _documentHeaderService.AddDocumentRowAsync(createRowDto, GetCurrentUser(), cancellationToken);

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
            if ((int)documentHeader.Status != (int)EntityDocumentStatus.Open)
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

            // Update the row quantity and notes
            rowEntity.Quantity = (int)rowDto.Quantity;
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
            if ((int)documentHeader.Status != (int)EntityDocumentStatus.Open)
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
                            _ = await _stockMovementService.ProcessAdjustmentMovementAsync(
                                productId: productId,
                                locationId: locationId,
                                adjustmentQuantity: adjustmentQuantity,
                                reason: "Inventory Count",
                                lotId: lotId,
                                notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                currentUser: GetCurrentUser(),
                                cancellationToken: cancellationToken);

                            _logger.LogInformation(
                                "Applied inventory adjustment for product {ProductId} at location {LocationId}: {Adjustment} (from {Current} to {New})",
                                productId, locationId, adjustmentQuantity, currentQuantity, newQuantity);

                            // 2) Ensure the Stocks table is updated to reflect the counted quantity
                            try
                            {
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

                                // Optionally update LastInventoryDate
                                if (updatedStock != null)
                                {
                                    await _stockService.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                                }
                            }
                            catch (Exception stockEx)
                            {
                                // Log and continue: movement exists, but Stocks update failed
                                _logger.LogError(stockEx, "Failed updating Stocks table for product {ProductId} at location {LocationId} after creating movement", productId, locationId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No adjustment needed for product {ProductId} at location {LocationId}: quantity unchanged at {Quantity}",
                                productId, locationId, currentQuantity);
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

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while finalizing inventory document {DocumentId}.", documentId);
            return CreateInternalServerErrorProblem("An error occurred while finalizing inventory document.", ex);
        }
    }

    #endregion
}