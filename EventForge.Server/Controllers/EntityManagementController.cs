using EventForge.DTOs.Common;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventForge.Server.Controllers;

/// <summary>
/// Consolidated REST API controller for managing common entities (Addresses, Contacts, References, Classification Nodes).
/// Provides unified CRUD operations with multi-tenant support and standardized patterns.
/// This controller replaces individual AddressesController, ContactsController, ReferencesController, and ClassificationNodesController
/// to reduce endpoint fragmentation and improve maintainability.
/// </summary>
[Route("api/v1/entities")]
[Authorize]
public class EntityManagementController : BaseApiController
{
    private readonly IAddressService _addressService;
    private readonly IContactService _contactService;
    private readonly IReferenceService _referenceService;
    private readonly IClassificationNodeService _classificationNodeService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EntityManagementController> _logger;

    public EntityManagementController(
        IAddressService addressService,
        IContactService contactService,
        IReferenceService referenceService,
        IClassificationNodeService classificationNodeService,
        ITenantContext tenantContext,
        ILogger<EntityManagementController> logger)
    {
        _addressService = addressService ?? throw new ArgumentNullException(nameof(addressService));
        _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
        _classificationNodeService = classificationNodeService ?? throw new ArgumentNullException(nameof(classificationNodeService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Address Management

    /// <summary>
    /// Retrieves all addresses with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of addresses</returns>
    /// <response code="200">Successfully retrieved addresses with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("addresses")]
    [ProducesResponseType(typeof(PagedResult<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AddressDto>>> GetAddresses(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _addressService.GetAddressesAsync(pagination, cancellationToken);
            
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
            _logger.LogError(ex, "An error occurred while retrieving addresses.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving addresses.", ex);
        }
    }

    /// <summary>
    /// Gets addresses by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses for the owner</returns>
    /// <response code="200">Returns the addresses for the owner</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("addresses/owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddressesByOwner(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var addresses = await _addressService.GetAddressesByOwnerAsync(ownerId, cancellationToken);
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving addresses for owner.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving addresses for owner.", ex);
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
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("addresses/{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddressDto>> GetAddress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var address = await _addressService.GetAddressByIdAsync(id, cancellationToken);
            if (address == null)
            {
                return CreateNotFoundProblem($"Address with ID {id} not found.");
            }

            return Ok(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the address.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the address.", ex);
        }
    }

    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name="createAddressDto">Address creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created address information</returns>
    /// <response code="201">Address created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("addresses")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddressDto>> CreateAddress(
        [FromBody] CreateAddressDto createAddressDto,
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
            var result = await _addressService.CreateAddressAsync(createAddressDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetAddress), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the address.");
            return CreateInternalServerErrorProblem("An error occurred while creating the address.", ex);
        }
    }

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="updateAddressDto">Address update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated address information</returns>
    /// <response code="200">Address updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the address is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("addresses/{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        Guid id,
        [FromBody] UpdateAddressDto updateAddressDto,
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
            var result = await _addressService.UpdateAddressAsync(id, updateAddressDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Address with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the address.");
            return CreateInternalServerErrorProblem("An error occurred while updating the address.", ex);
        }
    }

    /// <summary>
    /// Deletes an address (soft delete).
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Address deleted successfully</response>
    /// <response code="404">If the address is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("addresses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAddress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var deleted = await _addressService.DeleteAddressAsync(id, GetCurrentUser(), cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Address with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the address.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the address.", ex);
        }
    }

    #endregion

    #region Contact Management

    /// <summary>
    /// Retrieves all contacts with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contacts</returns>
    /// <response code="200">Successfully retrieved contacts with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("contacts")]
    [ProducesResponseType(typeof(PagedResult<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ContactDto>>> GetContacts(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _contactService.GetContactsAsync(pagination, cancellationToken);
            
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
            _logger.LogError(ex, "An error occurred while retrieving contacts.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving contacts.", ex);
        }
    }

    /// <summary>
    /// Gets contacts by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of contacts for the owner</returns>
    /// <response code="200">Returns the contacts for the owner</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("contacts/owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetContactsByOwner(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var contacts = await _contactService.GetContactsByOwnerAsync(ownerId, cancellationToken);
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving contacts for owner.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving contacts for owner.", ex);
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
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("contacts/{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContactDto>> GetContact(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var contact = await _contactService.GetContactByIdAsync(id, cancellationToken);
            if (contact == null)
            {
                return CreateNotFoundProblem($"Contact with ID {id} not found.");
            }

            return Ok(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the contact.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the contact.", ex);
        }
    }

    /// <summary>
    /// Creates a new contact.
    /// </summary>
    /// <param name="createContactDto">Contact creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created contact information</returns>
    /// <response code="201">Contact created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContactDto>> CreateContact(
        [FromBody] CreateContactDto createContactDto,
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
            var result = await _contactService.CreateContactAsync(createContactDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetContact), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the contact.");
            return CreateInternalServerErrorProblem("An error occurred while creating the contact.", ex);
        }
    }

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="updateContactDto">Contact update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated contact information</returns>
    /// <response code="200">Contact updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the contact is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("contacts/{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContactDto>> UpdateContact(
        Guid id,
        [FromBody] UpdateContactDto updateContactDto,
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
            var result = await _contactService.UpdateContactAsync(id, updateContactDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Contact with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the contact.");
            return CreateInternalServerErrorProblem("An error occurred while updating the contact.", ex);
        }
    }

    /// <summary>
    /// Deletes a contact (soft delete).
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Contact deleted successfully</response>
    /// <response code="404">If the contact is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("contacts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteContact(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var deleted = await _contactService.DeleteContactAsync(id, GetCurrentUser(), cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Contact with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the contact.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the contact.", ex);
        }
    }

    #endregion

    #region Reference Management

    /// <summary>
    /// Gets all references with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of references</returns>
    /// <response code="200">Returns the paginated list of references</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("references")]
    [ProducesResponseType(typeof(PagedResult<ReferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReferenceDto>>> GetReferences(
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
            var result = await _referenceService.GetReferencesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving references.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving references.", ex);
        }
    }

    /// <summary>
    /// Gets references by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of references for the owner</returns>
    /// <response code="200">Returns the references for the owner</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("references/owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ReferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ReferenceDto>>> GetReferencesByOwner(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var references = await _referenceService.GetReferencesByOwnerAsync(ownerId, cancellationToken);
            return Ok(references);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving references for owner.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving references for owner.", ex);
        }
    }

    /// <summary>
    /// Gets a reference by ID.
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reference information</returns>
    /// <response code="200">Returns the reference</response>
    /// <response code="404">If the reference is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("references/{id:guid}")]
    [ProducesResponseType(typeof(ReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReferenceDto>> GetReference(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var reference = await _referenceService.GetReferenceByIdAsync(id, cancellationToken);
            if (reference == null)
            {
                return CreateNotFoundProblem($"Reference with ID {id} not found.");
            }

            return Ok(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the reference.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the reference.", ex);
        }
    }

    /// <summary>
    /// Creates a new reference.
    /// </summary>
    /// <param name="createReferenceDto">Reference creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created reference information</returns>
    /// <response code="201">Reference created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("references")]
    [ProducesResponseType(typeof(ReferenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReferenceDto>> CreateReference(
        [FromBody] CreateReferenceDto createReferenceDto,
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
            var result = await _referenceService.CreateReferenceAsync(createReferenceDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetReference), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the reference.");
            return CreateInternalServerErrorProblem("An error occurred while creating the reference.", ex);
        }
    }

    /// <summary>
    /// Updates an existing reference.
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="updateReferenceDto">Reference update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated reference information</returns>
    /// <response code="200">Reference updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the reference is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("references/{id:guid}")]
    [ProducesResponseType(typeof(ReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReferenceDto>> UpdateReference(
        Guid id,
        [FromBody] UpdateReferenceDto updateReferenceDto,
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
            var result = await _referenceService.UpdateReferenceAsync(id, updateReferenceDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Reference with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the reference.");
            return CreateInternalServerErrorProblem("An error occurred while updating the reference.", ex);
        }
    }

    /// <summary>
    /// Deletes a reference (soft delete).
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Reference deleted successfully</response>
    /// <response code="404">If the reference is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("references/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteReference(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var deleted = await _referenceService.DeleteReferenceAsync(id, GetCurrentUser(), cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Reference with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the reference.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the reference.", ex);
        }
    }

    #endregion

    #region Classification Node Management

    /// <summary>
    /// <summary>
    /// Retrieves all classification nodes with pagination and parent filtering
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="parentId">Optional parent ID to filter children</param>
    /// <param name="deleted">Filter for soft-deleted items: 'false' (default), 'true', or 'all'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of classification nodes</returns>
    /// <response code="200">Successfully retrieved classification nodes with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("classification-nodes")]
    [SoftDeleteFilter]
    [ProducesResponseType(typeof(PagedResult<ClassificationNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ClassificationNodeDto>>> GetClassificationNodes(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? parentId = null,
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
            var result = await _classificationNodeService.GetClassificationNodesAsync(pagination, parentId, cancellationToken);
            
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
            _logger.LogError(ex, "An error occurred while retrieving classification nodes.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving classification nodes.", ex);
        }
    }

    /// <summary>
    /// Gets root classification nodes (nodes without parents).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root classification nodes</returns>
    /// <response code="200">Returns the root classification nodes</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("classification-nodes/root")]
    [ProducesResponseType(typeof(IEnumerable<ClassificationNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ClassificationNodeDto>>> GetRootClassificationNodes(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var nodes = await _classificationNodeService.GetRootClassificationNodesAsync(cancellationToken);
            return Ok(nodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving root classification nodes.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving root classification nodes.", ex);
        }
    }

    /// <summary>
    /// Gets children of a classification node.
    /// </summary>
    /// <param name="id">Parent classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child classification nodes</returns>
    /// <response code="200">Returns the list of child nodes</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("classification-nodes/{id:guid}/children")]
    [ProducesResponseType(typeof(IEnumerable<ClassificationNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ClassificationNodeDto>>> GetClassificationNodeChildren(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var children = await _classificationNodeService.GetChildrenAsync(id, cancellationToken);
            return Ok(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving child classification nodes.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving child classification nodes.", ex);
        }
    }

    /// <summary>
    /// Gets a classification node by ID.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Classification node information</returns>
    /// <response code="200">Returns the classification node</response>
    /// <response code="404">If the classification node is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("classification-nodes/{id:guid}")]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClassificationNodeDto>> GetClassificationNode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var node = await _classificationNodeService.GetClassificationNodeByIdAsync(id, cancellationToken);
            if (node == null)
            {
                return CreateNotFoundProblem($"Classification node with ID {id} not found.");
            }

            return Ok(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the classification node.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the classification node.", ex);
        }
    }

    /// <summary>
    /// Creates a new classification node.
    /// </summary>
    /// <param name="createClassificationNodeDto">Classification node creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created classification node information</returns>
    /// <response code="201">Classification node created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("classification-nodes")]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClassificationNodeDto>> CreateClassificationNode(
        [FromBody] CreateClassificationNodeDto createClassificationNodeDto,
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
            var result = await _classificationNodeService.CreateClassificationNodeAsync(createClassificationNodeDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetClassificationNode), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the classification node.");
            return CreateInternalServerErrorProblem("An error occurred while creating the classification node.", ex);
        }
    }

    /// <summary>
    /// Updates an existing classification node.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="updateClassificationNodeDto">Classification node update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated classification node information</returns>
    /// <response code="200">Classification node updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the classification node is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("classification-nodes/{id:guid}")]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClassificationNodeDto>> UpdateClassificationNode(
        Guid id,
        [FromBody] UpdateClassificationNodeDto updateClassificationNodeDto,
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
            var result = await _classificationNodeService.UpdateClassificationNodeAsync(id, updateClassificationNodeDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Classification node with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the classification node.");
            return CreateInternalServerErrorProblem("An error occurred while updating the classification node.", ex);
        }
    }

    /// <summary>
    /// Deletes a classification node (soft delete).
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Classification node deleted successfully</response>
    /// <response code="404">If the classification node is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("classification-nodes/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteClassificationNode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var deleted = await _classificationNodeService.DeleteClassificationNodeAsync(id, GetCurrentUser(), Array.Empty<byte>(), cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Classification node with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the classification node.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the classification node.", ex);
        }
    }

    #endregion
}