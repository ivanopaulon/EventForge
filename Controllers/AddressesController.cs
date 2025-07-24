using EventForge.Models.Common;
using EventForge.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for common entities management (Address, Contact, Reference, ClassificationNode).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AddressesController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressesController(IAddressService addressService)
    {
        _addressService = addressService ?? throw new ArgumentNullException(nameof(addressService));
    }

    /// <summary>
    /// Gets all addresses with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of addresses</returns>
    /// <response code="200">Returns the paginated list of addresses</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AddressDto>>> GetAddresses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new { message = "Page number must be greater than 0." });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Page size must be between 1 and 100." });
        }

        try
        {
            var result = await _addressService.GetAddressesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving addresses.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets addresses by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses for the owner</returns>
    /// <response code="200">Returns the addresses for the owner</response>
    [HttpGet("owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddressesByOwner(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var addresses = await _addressService.GetAddressesByOwnerAsync(ownerId, cancellationToken);
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving addresses.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets an address by ID.
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Address information</returns>
    /// <response code="200">Returns the address</response>
    /// <response code="404">If the address is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> GetAddress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var address = await _addressService.GetAddressByIdAsync(id, cancellationToken);

            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            return Ok(address);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the address.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name="createAddressDto">Address creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created address information</returns>
    /// <response code="201">Returns the created address</response>
    /// <response code="400">If the address data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddressDto>> CreateAddress(
        [FromBody] CreateAddressDto createAddressDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var address = await _addressService.CreateAddressAsync(createAddressDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetAddress),
                new { id = address.Id },
                address);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the address.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="updateAddressDto">Address update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated address information</returns>
    /// <response code="200">Returns the updated address</response>
    /// <response code="400">If the address data is invalid</response>
    /// <response code="404">If the address is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        Guid id,
        [FromBody] UpdateAddressDto updateAddressDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var address = await _addressService.UpdateAddressAsync(id, updateAddressDto, currentUser, cancellationToken);

            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            return Ok(address);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the address.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an address (soft delete).
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the address was successfully deleted</response>
    /// <response code="404">If the address is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _addressService.DeleteAddressAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the address.", error = ex.Message });
        }
    }
}