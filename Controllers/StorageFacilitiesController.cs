using EventForge.Models.Warehouse;
using EventForge.Services.Warehouse;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for storage facility management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StorageFacilitiesController : ControllerBase
{
    private readonly IStorageFacilityService _storageFacilityService;

    public StorageFacilitiesController(IStorageFacilityService storageFacilityService)
    {
        _storageFacilityService = storageFacilityService ?? throw new ArgumentNullException(nameof(storageFacilityService));
    }

    /// <summary>
    /// Gets all storage facilities with optional pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StorageFacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StorageFacilityDto>>> GetStorageFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest(new { message = "Page number must be greater than 0." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "Page size must be between 1 and 100." });

        try
        {
            var result = await _storageFacilityService.GetStorageFacilitiesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving storage facilities.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a storage facility by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StorageFacilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageFacilityDto>> GetStorageFacility(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facility = await _storageFacilityService.GetStorageFacilityByIdAsync(id, cancellationToken);

            if (facility == null)
                return NotFound(new { message = $"Storage facility with ID {id} not found." });

            return Ok(facility);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the storage facility.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new storage facility.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StorageFacilityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorageFacilityDto>> CreateStorageFacility(
        [FromBody] CreateStorageFacilityDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var facility = await _storageFacilityService.CreateStorageFacilityAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetStorageFacility),
                new { id = facility.Id },
                facility);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the storage facility.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing storage facility.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StorageFacilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageFacilityDto>> UpdateStorageFacility(
        Guid id,
        [FromBody] UpdateStorageFacilityDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var facility = await _storageFacilityService.UpdateStorageFacilityAsync(id, updateDto, currentUser, cancellationToken);

            if (facility == null)
                return NotFound(new { message = $"Storage facility with ID {id} not found." });

            return Ok(facility);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the storage facility.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a storage facility (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorageFacility(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _storageFacilityService.DeleteStorageFacilityAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Storage facility with ID {id} not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the storage facility.", error = ex.Message });
        }
    }
}