using EventForge.DTOs.Common;
using EventForge.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for contact management.
/// </summary>
[Route("api/v1/[controller]")]
public class ContactsController : BaseApiController
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
    }

    /// <summary>
    /// Gets all contacts with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contacts</returns>
    /// <response code="200">Returns the paginated list of contacts</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ContactDto>>> GetContacts(
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
            var result = await _contactService.GetContactsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving contacts.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets contacts by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of contacts for the owner</returns>
    /// <response code="200">Returns the contacts for the owner</response>
    [HttpGet("owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetContactsByOwner(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _contactService.GetContactsByOwnerAsync(ownerId, cancellationToken);
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving contacts.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a contact by ID.
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contact information</returns>
    /// <response code="200">Returns the contact</response>
    /// <response code="404">If the contact is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDto>> GetContact(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                return NotFound(new { message = $"Contact with ID {id} not found." });
            }

            return Ok(contact);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the contact.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new contact.
    /// </summary>
    /// <param name="createContactDto">Contact creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created contact information</returns>
    /// <response code="201">Returns the created contact</response>
    /// <response code="400">If the contact data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactDto>> CreateContact(
        [FromBody] CreateContactDto createContactDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var contact = await _contactService.CreateContactAsync(createContactDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetContact),
                new { id = contact.Id },
                contact);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the contact.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="updateContactDto">Contact update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated contact information</returns>
    /// <response code="200">Returns the updated contact</response>
    /// <response code="400">If the contact data is invalid</response>
    /// <response code="404">If the contact is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDto>> UpdateContact(
        Guid id,
        [FromBody] UpdateContactDto updateContactDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var contact = await _contactService.UpdateContactAsync(id, updateContactDto, currentUser, cancellationToken);

            if (contact == null)
            {
                return NotFound(new { message = $"Contact with ID {id} not found." });
            }

            return Ok(contact);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the contact.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a contact (soft delete).
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the contact was successfully deleted</response>
    /// <response code="404">If the contact is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _contactService.DeleteContactAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Contact with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the contact.", error = ex.Message });
        }
    }
}