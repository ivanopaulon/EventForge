using EventForge.DTOs.Warehouse;
using EventForge.Filters;
using EventForge.Services.Warehouse;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for storage location management.
/// </summary>
[Route("api/v1/[controller]")]
public class StorageLocationsController : BaseApiController
{
    private readonly IStorageLocationService _storageLocationService;

    public StorageLocationsController(IStorageLocationService storageLocationService)
    {
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
    }

    /// <summary>
    /// Gets all storage locations with optional pagination and warehouse filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="warehouseId">Optional warehouse ID to filter locations</param>
    /// <param name="deleted">Filter for soft-deleted items: 'false' (default), 'true', or 'all'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of storage locations</returns>
    /// <response code="200">Returns the paginated list of storage locations</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [SoftDeleteFilter]
    [ProducesResponseType(typeof(PagedResult<StorageLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StorageLocationDto>>> GetStorageLocations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string deleted = "false",
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page number must be greater than 0.");
            return CreateValidationProblemDetails();
        }

        if (pageSize < 1 || pageSize > 100)
        {
            ModelState.AddModelError(nameof(pageSize), "Page size must be between 1 and 100.");
            return CreateValidationProblemDetails();
        }

        var result = await _storageLocationService.GetStorageLocationsAsync(page, pageSize, warehouseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets storage locations for a specific warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of storage locations for the warehouse</returns>
    /// <response code="200">Returns the list of storage locations</response>
    [HttpGet("warehouse/{warehouseId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StorageLocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StorageLocationDto>>> GetLocationsByWarehouse(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var result = await _storageLocationService.GetLocationsByWarehouseAsync(warehouseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets available storage locations (with remaining capacity).
    /// </summary>
    /// <param name="warehouseId">Optional warehouse ID to filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available storage locations</returns>
    /// <response code="200">Returns the list of available storage locations</response>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<StorageLocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StorageLocationDto>>> GetAvailableLocations(
        [FromQuery] Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _storageLocationService.GetAvailableLocationsAsync(warehouseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a storage location by ID.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage location information</returns>
    /// <response code="200">Returns the storage location</response>
    /// <response code="404">If the storage location is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageLocationDto>> GetStorageLocation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await _storageLocationService.GetStorageLocationByIdAsync(id, cancellationToken);

        if (location == null)
        {
            return CreateNotFoundProblem($"Storage location with ID {id} not found.");
        }

        return Ok(location);
    }

    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    /// <param name="createDto">Storage location creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created storage location</returns>
    /// <response code="201">Returns the newly created storage location</response>
    /// <response code="400">If the storage location data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorageLocationDto>> CreateStorageLocation(
        [FromBody] CreateStorageLocationDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var currentUser = GetCurrentUser();
        var location = await _storageLocationService.CreateStorageLocationAsync(createDto, currentUser, cancellationToken);

        return CreatedAtAction(
            nameof(GetStorageLocation),
            new { id = location.Id },
            location);
    }

    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="updateDto">Storage location update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated storage location</returns>
    /// <response code="200">Returns the updated storage location</response>
    /// <response code="400">If the storage location data is invalid</response>
    /// <response code="404">If the storage location is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageLocationDto>> UpdateStorageLocation(
        Guid id,
        [FromBody] UpdateStorageLocationDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var currentUser = GetCurrentUser();
        var location = await _storageLocationService.UpdateStorageLocationAsync(id, updateDto, currentUser, cancellationToken);

        if (location == null)
        {
            return CreateNotFoundProblem($"Storage location with ID {id} not found.");
        }

        return Ok(location);
    }

    /// <summary>
    /// Updates the occupancy of a storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="occupancy">New occupancy value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated storage location</returns>
    /// <response code="200">Returns the updated storage location</response>
    /// <response code="400">If the occupancy value is invalid</response>
    /// <response code="404">If the storage location is not found</response>
    [HttpPatch("{id:guid}/occupancy")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageLocationDto>> UpdateOccupancy(
        Guid id,
        [FromBody] int occupancy,
        CancellationToken cancellationToken = default)
    {
        if (occupancy < 0)
        {
            ModelState.AddModelError(nameof(occupancy), "Occupancy must be non-negative.");
            return CreateValidationProblemDetails();
        }

        var currentUser = GetCurrentUser();
        var location = await _storageLocationService.UpdateOccupancyAsync(id, occupancy, currentUser, cancellationToken);

        if (location == null)
        {
            return CreateNotFoundProblem($"Storage location with ID {id} not found.");
        }

        return Ok(location);
    }

    /// <summary>
    /// Deletes a storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the storage location was successfully deleted</response>
    /// <response code="404">If the storage location is not found</response>
    /// <response code="400">If the storage location cannot be deleted (contains inventory)</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteStorageLocation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await _storageLocationService.DeleteStorageLocationAsync(id, currentUser, Array.Empty<byte>(), cancellationToken);

        if (!result)
        {
            return CreateNotFoundProblem($"Storage location with ID {id} not found.");
        }

        return NoContent();
    }
}