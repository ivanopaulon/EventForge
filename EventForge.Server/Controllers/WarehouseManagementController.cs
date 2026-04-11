using EventForge.DTOs.Documents;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
public class WarehouseManagementController(
    IWarehouseFacade warehouseFacade,
    ITenantContext tenantContext,
    ILogger<WarehouseManagementController> logger) : BaseApiController
{
    // PERFORMANCE PROTECTION: Maximum page size for bulk operations to prevent performance issues
    // Large page sizes can cause:
    // - Memory exhaustion (loading too many entities)
    // - Database timeouts (long-running queries)
    // - API response timeouts (serialization overhead)
    // 
    // COMPLIANCE: This limit aligns with database performance best practices
    // and prevents accidental DoS-style resource consumption.
    private const int MaxBulkOperationPageSize = 1000;

    #region Helper Methods

    // PERFORMANCE ESTIMATION CONSTANTS
    // These values are used to calculate estimated processing times for bulk operations
    // and provide user feedback during long-running document processing.
    // 
    // BUSINESS RULE: Prevents timeout issues by warning users when operations
    // will take longer than expected, allowing them to adjust batch sizes.
    // 
    // NOTE: These could be made configurable through appsettings.json in future if needed
    private const double ESTIMATED_SECONDS_PER_ROW = 0.01;
    private const int LARGE_DOCUMENT_THRESHOLD = 300;
    private const int MAX_DISPLAYED_MISSING_IDS = 5;
    private const double MIN_ESTIMATED_LOAD_TIME_SECONDS = 1.0;

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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetStorageFacilitiesAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var facility = await warehouseFacade.GetStorageFacilityByIdAsync(id, cancellationToken);
            if (facility is null)
            {
                return CreateNotFoundProblem($"Storage facility with ID {id} not found.");
            }

            return Ok(facility);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateStorageFacilityAsync(createStorageFacilityDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetStorageFacility), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetStorageLocationsAsync(pagination, facilityId, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var location = await warehouseFacade.GetStorageLocationByIdAsync(id, cancellationToken);
            if (location is null)
            {
                return CreateNotFoundProblem($"Storage location with ID {id} not found.");
            }

            return Ok(location);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateStorageLocationAsync(createStorageLocationDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetStorageLocation), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotsAsync(pagination, productId, status, expiringSoon, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotByIdAsync(id, cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotByCodeAsync(code, cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetExpiringLotsAsync(daysAhead, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateLotAsync(createLotDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetLot), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UpdateLotAsync(id, updateLotDto, GetCurrentUser(), cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.DeleteLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UpdateQualityStatusAsync(id, qualityStatus, GetCurrentUser(), notes, cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.BlockLotAsync(id, reason, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UnblockLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetSerialsAsync(pagination.Page, pagination.PageSize, productId, lotId, locationId, status, searchTerm, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var serial = await warehouseFacade.GetSerialByIdAsync(id, cancellationToken);
            return serial is not null ? Ok(serial) : NotFound();
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateSerialAsync(createDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetSerialById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UpdateSerialStatusAsync(id, status, GetCurrentUser(), notes, cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
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
            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            // Convert to InventoryDocumentDto with enriched rows (only if requested)
            var inventoryDocuments = new List<InventoryDocumentDto>();
            foreach (var doc in documentsResult.Items)
            {
                // Enrich rows with complete product and location data using optimized batch method
                // Only enrich if rows were requested and are present
                var enrichedRows = includeRows && doc.Rows is not null && doc.Rows.Any()
                    ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(doc.Rows, cancellationToken)
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

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Enrich rows with complete product and location data
            var enrichedRows = documentHeader.Rows is not null && documentHeader.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(documentHeader.Rows, cancellationToken)
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return Problem("Tenant not found or invalid.", statusCode: StatusCodes.Status403Forbidden);
            }

            // Get or create an "Inventory" document type
            var inventoryDocumentType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(currentTenantId.Value, cancellationToken);

            // Get or create system business party for internal operations
            var systemBusinessPartyId = await warehouseFacade.GetOrCreateSystemBusinessPartyAsync(currentTenantId.Value, cancellationToken);

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

            var documentHeader = await warehouseFacade.CreateDocumentHeaderAsync(createHeaderDto, GetCurrentUser(), cancellationToken);

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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Get current stock level to calculate adjustment
            var existingStocks = await warehouseFacade.GetStockAsync(
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
            var product = await warehouseFacade.GetProductByIdAsync(rowDto.ProductId, cancellationToken);
            var location = await warehouseFacade.GetStorageLocationByIdAsync(rowDto.LocationId, cancellationToken);

            if (product is null)
            {
                return CreateNotFoundProblem($"Product with ID {rowDto.ProductId} was not found.");
            }

            if (location is null)
            {
                return CreateNotFoundProblem($"Location with ID {rowDto.LocationId} was not found.");
            }

            // Get unit of measure symbol if available
            string? unitOfMeasure = null;
            if (product.UnitOfMeasureId.HasValue)
            {
                try
                {
                    unitOfMeasure = await warehouseFacade.GetUnitOfMeasureSymbolAsync(product.UnitOfMeasureId.Value, cancellationToken);
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
                    var vatDetails = await warehouseFacade.GetVatRateDetailsAsync(product.VatRateId.Value, cancellationToken);
                    if (vatDetails.HasValue)
                    {
                        vatRate = vatDetails.Value.Percentage;
                        vatDescription = vatDetails.Value.Description;
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

            if (existingRow is not null)
            {
                // Row exists - merge by adding quantities together
                var newQuantity = existingRow.Quantity + rowDto.Quantity;


                // Update the existing row via facade
                documentRow = await warehouseFacade.UpdateOrMergeInventoryRowAsync(
                    documentId,
                    existingRow.Id,
                    newQuantity,
                    rowDto.Notes,
                    GetCurrentUser(),
                    cancellationToken);
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

                documentRow = await warehouseFacade.AddDocumentRowAsync(createRowDto, GetCurrentUser(), cancellationToken);
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
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data using the helper method
            var enrichedRows = updatedDocument?.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header to check status
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);

            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Only allow updating Draft documents (status is Open in entity)
            if (documentHeader.Status != DTOs.Common.DocumentStatus.Open)
            {
                return CreateValidationProblemDetails("Only Draft inventory documents can be updated. This document has already been finalized.");
            }

            // Update the document header fields via facade
            await warehouseFacade.UpdateDocumentHeaderFieldsAsync(
                documentId,
                updateDto.InventoryDate,
                updateDto.WarehouseId,
                updateDto.Notes,
                GetCurrentUser(),
                cancellationToken);

            logger.LogInformation("Updated inventory document {DocumentId} - Date: {Date}, Warehouse: {WarehouseId}",
                documentId, updateDto.InventoryDate, updateDto.WarehouseId);

            // Get the updated document with full details
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = updatedDocument!.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Open)
            {
                return CreateValidationProblemDetails("Cannot modify rows in a closed or cancelled inventory document.");
            }

            // Update the row via facade
            var updated = await warehouseFacade.UpdateInventoryRowAsync(
                rowId,
                rowDto.ProductId,
                rowDto.Quantity,
                rowDto.LocationId,
                rowDto.Notes,
                GetCurrentUser(),
                cancellationToken);

            if (!updated)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }

            // Get updated document
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
            var enrichedRows = updatedDocument?.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Open)
            {
                return CreateValidationProblemDetails("Cannot delete rows from a closed or cancelled inventory document.");
            }

            // Soft delete the row via facade
            var deleted = await warehouseFacade.DeleteInventoryRowAsync(rowId, GetCurrentUser(), cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }

            // Get updated document
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
            var enrichedRows = updatedDocument?.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header with rows
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Validate document is in Open status
            if (documentHeader.Status != EventForge.DTOs.Common.DocumentStatus.Open)
            {
                logger.LogWarning(
                    "Cannot finalize inventory document {DocumentId}: status is {Status}, expected Open",
                    documentId, documentHeader.Status);

                return CreateValidationProblemDetails($"Cannot finalize document: status is '{documentHeader.Status}'. Only documents in 'Open' status can be finalized.");
            }

            // Validate document has rows
            if (documentHeader.Rows is null || !documentHeader.Rows.Any())
            {
                logger.LogWarning(
                    "Inventory document {DocumentId} has no rows to process",
                    documentId);

                return CreateValidationProblemDetails("Cannot finalize an inventory document with no rows.");
            }

            // Start timing and initialize counters
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var totalRows = documentHeader.Rows.Count;
            var processedRows = 0;
            var skippedRows = 0;


            // Validation: verify that all ProductId and LocationId exist before processing
            var productIds = documentHeader.Rows.Where(r => r.ProductId.HasValue).Select(r => r.ProductId!.Value).Distinct().ToList();
            var locationIds = documentHeader.Rows.Where(r => r.LocationId.HasValue).Select(r => r.LocationId!.Value).Distinct().ToList();

            var missingProducts = await warehouseFacade.ValidateProductsExistAsync(productIds, cancellationToken);
            if (missingProducts.Any())
            {
                return CreateValidationProblemDetails($"Document contains {missingProducts.Count} non-existent product(s). Cannot finalize.");
            }

            // Validate locations
            var missingLocations = await warehouseFacade.ValidateLocationsExistAsync(locationIds, cancellationToken);
            if (missingLocations.Any())
            {
                return CreateValidationProblemDetails($"Document contains {missingLocations.Count} non-existent location(s). Cannot finalize.");
            }

            // Process each row and apply stock adjustments
            if (documentHeader.Rows is not null && documentHeader.Rows.Any())
            {

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
                            logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                            skippedRows++;
                            continue;
                        }

                        var newQuantity = row.Quantity;

                        // Get current stock level
                        var existingStocks = await warehouseFacade.GetStockAsync(
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
                            _ = await warehouseFacade.ProcessAdjustmentMovementAsync(
                                productId: productId,
                                locationId: locationId,
                                adjustmentQuantity: adjustmentQuantity,
                                reason: "Inventory Count",
                                lotId: lotId,
                                notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                currentUser: GetCurrentUser(),
                                movementDate: documentHeader.Date,
                                cancellationToken: cancellationToken);


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
                            var updatedStock = await warehouseFacade.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);

                            // Verify stock was successfully created/updated
                            if (updatedStock is not null)
                            {
                                await warehouseFacade.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                            }
                            else
                            {
                                // If stock creation/update fails, this is a critical error - propagate it
                                throw new InvalidOperationException($"Failed to create or update stock for product {productId} at location {locationId}");
                            }
                        }
                        else
                        {
                            processedRows++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}",
                            row.Id, documentId);
                        // Continue processing other rows even if one fails
                    }
                }
            }

            // Now close the document
            var closedDocument = await warehouseFacade.CloseDocumentAsync(documentId, GetCurrentUser(), cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = closedDocument!.Rows is not null && closedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
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
            logger.LogInformation(
                "Completed finalization of inventory document {DocumentId} ({DocumentNumber}) in {ElapsedMs}ms. " +
                "Rows processed: {ProcessedRows}, Rows skipped: {SkippedRows}, Total: {TotalRows}",
                documentId, documentHeader.Number, stopwatch.ElapsedMilliseconds,
                processedRows, skippedRows, totalRows);

            return Ok(result);
        }
        catch (Exception ex)
        {
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

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.SeedInventoryAsync(
                request,
                GetCurrentUser(),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Operation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = new InventoryValidationResultDto
            {
                DocumentId = documentId,
                Timestamp = DateTime.UtcNow,
                IsValid = true,
                Issues = new List<InventoryValidationIssue>(),
                Stats = new InventoryStats()
            };

            // 1. Verify document exists
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // 2. Count total rows without loading them
            var totalRows = await warehouseFacade.CountDocumentRowsAsync(documentId, cancellationToken);

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
            var rowsWithNullData = await warehouseFacade.GetRowsWithNullDataAsync(documentId, cancellationToken);

            foreach (var row in rowsWithNullData)
            {
                var missingFields = new List<string>();
                if (row.ProductId is null) missingFields.Add("ProductId");
                if (row.LocationId is null) missingFields.Add("LocationId");

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
            var (productIds, locationIds) = await warehouseFacade.GetUniqueProductAndLocationIdsAsync(documentId, cancellationToken);

            result.Stats.UniqueProducts = productIds.Count;
            result.Stats.UniqueLocations = locationIds.Count;

            // 5. Verify referenced products exist
            if (productIds.Any())
            {
                var missingProductIds = await warehouseFacade.ValidateProductsExistAsync(productIds, cancellationToken);
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
                var missingLocationIds = await warehouseFacade.ValidateLocationsExistAsync(locationIds, cancellationToken);
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
            logger.LogInformation(
                "Completed validation for document {DocumentId} in {ElapsedMs}ms. " +
                "Total rows: {TotalRows}, Issues: {IssueCount}, Valid: {IsValid}",
                documentId, stopwatch.ElapsedMilliseconds, totalRows, result.Issues.Count, result.IsValid);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
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

            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var inventoryDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items is not null)
            {
                foreach (var doc in documentsResult.Items.OrderByDescending(d => d.CreatedAt))
                {
                    // Enrich rows with product and location data
                    var enrichedRows = doc.Rows is not null && doc.Rows.Any()
                        ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(doc.Rows, cancellationToken)
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
            return CreateInternalServerErrorProblem("An error occurred while retrieving open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Returns lightweight headers (no rows) of all Open inventory documents for the current tenant.
    /// RowCount is calculated via SQL COUNT — no rows are loaded into memory, safe for any number of documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of lightweight open inventory document headers</returns>
    /// <response code="200">Returns the list of open inventory document headers</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/open-headers")]
    [ProducesResponseType(typeof(List<InventoryDocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentHeaderDto>>> GetOpenInventoryDocumentHeaders(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var headers = await warehouseFacade.GetOpenInventoryDocumentHeadersAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            return Ok(headers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving open inventory document headers.", ex);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Cancel the document via facade
            var cancelled = await warehouseFacade.CancelInventoryDocumentAsync(documentId, GetCurrentUser(), cancellationToken);

            if (!cancelled)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            logger.LogInformation("Cancelled inventory document {DocumentId} without applying adjustments", documentId);

            return Ok();
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        using var transaction = await warehouseFacade.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // Get all open inventory documents
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (EventForge.DTOs.Common.DocumentStatus)(int)DocumentStatus.Open,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = false
            };

            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var finalizedDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items is not null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();

                foreach (var doc in itemsList)
                {
                    // Call the existing finalize logic for each document
                    // We need to get the result as InventoryDocumentDto
                    var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(doc.Id, includeRows: true, cancellationToken);

                    if (documentHeader is not null)
                    {
                        // Process each row and apply stock adjustments (reuse logic from FinalizeInventoryDocument)
                        if (documentHeader.Rows is not null && documentHeader.Rows.Any())
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
                                        logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                                        continue;
                                    }

                                    var newQuantity = row.Quantity;
                                    var existingStocks = await warehouseFacade.GetStockAsync(
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
                                        _ = await warehouseFacade.ProcessAdjustmentMovementAsync(
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

                                        var updatedStock = await warehouseFacade.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);
                                        if (updatedStock is not null)
                                        {
                                            await warehouseFacade.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}", row.Id, doc.Id);
                                    throw; // Re-throw to trigger transaction rollback
                                }
                            }
                        }

                        // Close the document
                        var closedDocument = await warehouseFacade.CloseDocumentAsync(doc.Id, GetCurrentUser(), cancellationToken);

                        // Enrich rows with product and location data
                        var enrichedRows = closedDocument!.Rows is not null && closedDocument.Rows.Any()
                            ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
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
            logger.LogInformation("Successfully finalized {Count} inventory documents", finalizedDocuments.Count);

            return Ok(finalizedDocuments);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {

            // 1. Verify document exists
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // 2-3. Get paginated rows via facade
            var (documentRows, totalRows) = await warehouseFacade.GetDocumentRowsPagedAsync(
                documentId,
                pagination.Page,
                pagination.PageSize,
                cancellationToken);

            // 4. Enrich using optimized batch method
            var enrichedRows = await warehouseFacade.EnrichInventoryDocumentRowsAsync(documentRows, cancellationToken);

            var result = new PagedResult<InventoryDocumentRowDto>
            {
                Items = enrichedRows,
                TotalCount = totalRows,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };


            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get all open inventory documents
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (EventForge.DTOs.Common.DocumentStatus)(int)DocumentStatus.Open,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = false
            };

            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            int cancelledCount = 0;

            if (documentsResult?.Items is not null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();

                // Cancel all documents in batch via facade
                var documentIds = itemsList.Select(d => d.Id).ToList();
                cancelledCount = await warehouseFacade.CancelInventoryDocumentsBatchAsync(documentIds, GetCurrentUser(), cancellationToken);

                logger.LogInformation("Successfully cancelled {Count} inventory documents without applying adjustments", cancelledCount);
            }

            return Ok(cancelledCount);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var report = await warehouseFacade.DiagnoseDocumentAsync(documentId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.AutoRepairDocumentAsync(documentId, options, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var success = await warehouseFacade.RepairRowAsync(documentId, rowId, repairData, GetCurrentUser(), cancellationToken);
            if (!success)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var removedCount = await warehouseFacade.RemoveProblematicRowsAsync(documentId, rowIds, GetCurrentUser(), cancellationToken);
            return Ok(removedCount);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing problematic rows.", ex);
        }
    }

    /// <summary>
    /// Returns a preview of the merge operation for the selected inventory documents.
    /// Use this before calling /merge to show the user what will happen.
    /// No data is modified by this call.
    /// </summary>
    /// <param name="documentIds">List of inventory document IDs to preview</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview of the merge operation</returns>
    /// <response code="200">Returns the merge preview</response>
    /// <response code="400">If validation fails</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/merge-preview")]
    [ProducesResponseType(typeof(MergeInventoryDocumentsPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewMergeInventoryDocuments(
        [FromBody] List<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            if (documentIds is null || documentIds.Count < 2)
            {
                ModelState.AddModelError("documentIds", "At least 2 documents are required to preview a merge.");
                return CreateValidationProblemDetails();
            }


            var preview = await warehouseFacade.PreviewMergeInventoryDocumentsAsync(documentIds, cancellationToken);

            if (preview.SourceDocuments.Count != documentIds.Count)
            {
                ModelState.AddModelError("documentIds", "One or more source documents not found.");
                return CreateValidationProblemDetails();
            }

            return Ok(preview);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing the inventory document merge.", ex);
        }
    }

    /// <summary>
    /// Merges the selected inventory documents into one finalized document.
    /// Source documents (excluding the target/base) are soft-deleted.
    /// Row merging: ProductId + LocationId matching => quantities summed.
    /// </summary>
    /// <param name="mergeDto">Merge request with source document IDs and optional target document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the merge operation</returns>
    /// <response code="200">Returns the merge result</response>
    /// <response code="400">If validation fails</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/merge")]
    [ProducesResponseType(typeof(MergeInventoryDocumentsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MergeInventoryDocuments(
        [FromBody] MergeInventoryDocumentsDto mergeDto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            if (mergeDto.SourceDocumentIds.Count < 2)
            {
                ModelState.AddModelError("SourceDocumentIds", "At least 2 documents are required to merge.");
                return CreateValidationProblemDetails();
            }

            if (mergeDto.TargetDocumentId.HasValue && !mergeDto.SourceDocumentIds.Contains(mergeDto.TargetDocumentId.Value))
            {
                ModelState.AddModelError("TargetDocumentId", "TargetDocumentId must be included in SourceDocumentIds. SourceDocumentIds should contain all documents to merge, including the target.");
                return CreateValidationProblemDetails();
            }


            var result = await warehouseFacade.MergeInventoryDocumentsAsync(mergeDto, GetCurrentUser(), cancellationToken);

            logger.LogInformation(
                "Merged inventory documents into {MergedNumber}. TotalRows: {TotalRows}, MergedRows: {MergedRows}, CopiedRows: {CopiedRows}, SoftDeleted: {SoftDeleted}.",
                result.MergedDocumentNumber, result.TotalRows, result.MergedRows, result.CopiedRows, result.SoftDeletedDocumentIds.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
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

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await warehouseFacade.CalculateReconciledStockAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
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

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Unknown";
            var result = await warehouseFacade.ApplyReconciliationAsync(request, currentUser, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
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

            var fileBytes = await warehouseFacade.ExportReconciliationReportAsync(request, cancellationToken);

            if (fileBytes is null || fileBytes.Length == 0)
            {
                logger.LogWarning("Export generated no data or feature not yet implemented");
                return StatusCode(501, new { message = "Excel export feature not yet implemented" });
            }

            var fileName = $"StockReconciliation_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while exporting stock reconciliation report.", ex);
        }
    }

    /// <summary>
    /// Previews which stock movements would be rebuilt from approved/closed documents (dry-run).
    /// Does NOT create any movements - returns a preview of what would be created.
    /// </summary>
    /// <param name="request">Rebuild request with optional filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview result showing rows that would have movements created</returns>
    [HttpPost("stock-reconciliation/rebuild-movements/preview")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(RebuildMovementsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RebuildMovementsPreview(
        [FromBody] RebuildMovementsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.DryRun = true; // force dry-run for preview
            var result = await warehouseFacade.RebuildMissingMovementsFromDocumentsAsync(
                request, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing stock movement rebuild.", ex);
        }
    }

    /// <summary>
    /// Rebuilds missing stock movements from approved/closed documents.
    /// Creates stock movements for document rows that do not yet have a corresponding movement.
    /// </summary>
    /// <param name="request">Rebuild request with optional filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result showing created, skipped, and failed movements</returns>
    [HttpPost("stock-reconciliation/rebuild-movements/execute")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(RebuildMovementsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RebuildMovementsExecute(
        [FromBody] RebuildMovementsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.DryRun = false; // force execute
            // Use CancellationToken.None for write operations: once the rebuild begins
            // committing movements we must not abort mid-flight if the client disconnects
            // or times out, as that would leave stock data in an inconsistent state.
            var result = await warehouseFacade.RebuildMissingMovementsFromDocumentsAsync(
                request, GetCurrentUser(), CancellationToken.None);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while executing stock movement rebuild.", ex);
        }
    }

    #endregion

    #region Export Operations

    /// <summary>
    /// Export all warehouses to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("facilities/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportWarehouses(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await warehouseFacade.GetWarehousesForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await warehouseFacade.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Warehouses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await warehouseFacade.ExportToExcelAsync(data, "Warehouses", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Warehouses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Export all inventory to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("inventory/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await warehouseFacade.GetInventoryForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await warehouseFacade.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await warehouseFacade.ExportToExcelAsync(data, "Inventory", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Performs a bulk warehouse transfer operation.
    /// </summary>
    /// <param name="bulkTransferDto">Bulk transfer request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk transfer operation</returns>
    /// <response code="200">Returns the result of the bulk transfer operation</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("bulk-transfer")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(EventForge.DTOs.Bulk.BulkTransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventForge.DTOs.Bulk.BulkTransferResultDto>> BulkTransfer(
        [FromBody] EventForge.DTOs.Bulk.BulkTransferDto bulkTransferDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "System";
            var result = await warehouseFacade.BulkTransferAsync(bulkTransferDto, currentUser, cancellationToken);

            logger.LogInformation(
                "Bulk transfer: {SuccessCount} successful, {FailedCount} failed",
                result.SuccessCount, result.FailedCount);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bulk transfer request");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during bulk transfer.", ex);
        }
    }

    #endregion
}