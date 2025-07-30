using EventForge.DTOs.Warehouse;
using EventForge.DTOs.Common;
using EventForge.Server.Filters;
using EventForge.Server.Services.Warehouse;
using EventForge.Server.Services.Tenants;
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
public class WarehouseManagementController : BaseApiController
{
    private readonly IStorageFacilityService _storageFacilityService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ITenantContext _tenantContext;

    public WarehouseManagementController(
        IStorageFacilityService storageFacilityService,
        IStorageLocationService storageLocationService,
        ITenantContext tenantContext)
    {
        _storageFacilityService = storageFacilityService ?? throw new ArgumentNullException(nameof(storageFacilityService));
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

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
            return CreateInternalServerErrorProblem("An error occurred while creating the storage location.", ex);
        }
    }

    #endregion
}