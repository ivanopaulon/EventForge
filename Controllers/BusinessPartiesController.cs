using EventForge.DTOs.Business;
using EventForge.Services.Business;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for business party and business party accounting management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BusinessPartiesController : ControllerBase
{
    private readonly IBusinessPartyService _businessPartyService;

    public BusinessPartiesController(IBusinessPartyService businessPartyService)
    {
        _businessPartyService = businessPartyService ?? throw new ArgumentNullException(nameof(businessPartyService));
    }

    #region BusinessParty Endpoints

    /// <summary>
    /// Gets all business parties with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business parties</returns>
    /// <response code="200">Returns the paginated list of business parties</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BusinessPartyDto>>> GetBusinessParties(
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
            var result = await _businessPartyService.GetBusinessPartiesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving business parties.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets a business party by ID.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party details</returns>
    /// <response code="200">Returns the business party</response>
    /// <response code="404">If the business party is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartyDto>> GetBusinessParty(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessParty = await _businessPartyService.GetBusinessPartyByIdAsync(id, cancellationToken);

            if (businessParty == null)
            {
                return NotFound(new { message = $"Business party with ID {id} not found." });
            }

            return Ok(businessParty);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the business party.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets business parties by type.
    /// </summary>
    /// <param name="partyType">Business party type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business parties of the specified type</returns>
    /// <response code="200">Returns the list of business parties</response>
    [HttpGet("by-type/{partyType}")]
    [ProducesResponseType(typeof(IEnumerable<BusinessPartyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BusinessPartyDto>>> GetBusinessPartiesByType(BusinessPartyType partyType, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessParties = await _businessPartyService.GetBusinessPartiesByTypeAsync(partyType, cancellationToken);
            return Ok(businessParties);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving business parties by type.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new business party.
    /// </summary>
    /// <param name="createBusinessPartyDto">Business party creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party</returns>
    /// <response code="201">Returns the newly created business party</response>
    /// <response code="400">If the business party data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BusinessPartyDto>> CreateBusinessParty(CreateBusinessPartyDto createBusinessPartyDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var businessParty = await _businessPartyService.CreateBusinessPartyAsync(createBusinessPartyDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetBusinessParty), new { id = businessParty.Id }, businessParty);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the business party.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing business party.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="updateBusinessPartyDto">Business party update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party</returns>
    /// <response code="200">Returns the updated business party</response>
    /// <response code="400">If the business party data is invalid</response>
    /// <response code="404">If the business party is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartyDto>> UpdateBusinessParty(Guid id, UpdateBusinessPartyDto updateBusinessPartyDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var businessParty = await _businessPartyService.UpdateBusinessPartyAsync(id, updateBusinessPartyDto, currentUser, cancellationToken);

            if (businessParty == null)
            {
                return NotFound(new { message = $"Business party with ID {id} not found." });
            }

            return Ok(businessParty);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the business party.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a business party (soft delete).
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Business party deleted successfully</response>
    /// <response code="404">If the business party is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBusinessParty(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var deleted = await _businessPartyService.DeleteBusinessPartyAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Business party with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the business party.", detail = ex.Message });
        }
    }

    #endregion

    #region BusinessPartyAccounting Endpoints

    /// <summary>
    /// Gets all business party accounting records with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party accounting records</returns>
    /// <response code="200">Returns the paginated list of business party accounting records</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet("accounting")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyAccountingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BusinessPartyAccountingDto>>> GetBusinessPartyAccounting(
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
            var result = await _businessPartyService.GetBusinessPartyAccountingAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving business party accounting records.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets a business party accounting record by ID.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting details</returns>
    /// <response code="200">Returns the business party accounting record</response>
    /// <response code="404">If the business party accounting record is not found</response>
    [HttpGet("accounting/{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> GetBusinessPartyAccounting(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessPartyAccounting = await _businessPartyService.GetBusinessPartyAccountingByIdAsync(id, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return NotFound(new { message = $"Business party accounting with ID {id} not found." });
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the business party accounting record.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets business party accounting by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting details</returns>
    /// <response code="200">Returns the business party accounting record</response>
    /// <response code="404">If the business party accounting record is not found</response>
    [HttpGet("{businessPartyId:guid}/accounting")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> GetBusinessPartyAccountingByBusinessPartyId(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessPartyAccounting = await _businessPartyService.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return NotFound(new { message = $"Business party accounting for business party {businessPartyId} not found." });
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the business party accounting record.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new business party accounting record.
    /// </summary>
    /// <param name="createBusinessPartyAccountingDto">Business party accounting creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party accounting record</returns>
    /// <response code="201">Returns the newly created business party accounting record</response>
    /// <response code="400">If the business party accounting data is invalid</response>
    [HttpPost("accounting")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> CreateBusinessPartyAccounting(CreateBusinessPartyAccountingDto createBusinessPartyAccountingDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var businessPartyAccounting = await _businessPartyService.CreateBusinessPartyAccountingAsync(createBusinessPartyAccountingDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetBusinessPartyAccounting), new { id = businessPartyAccounting.Id }, businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the business party accounting record.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing business party accounting record.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="updateBusinessPartyAccountingDto">Business party accounting update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party accounting record</returns>
    /// <response code="200">Returns the updated business party accounting record</response>
    /// <response code="400">If the business party accounting data is invalid</response>
    /// <response code="404">If the business party accounting record is not found</response>
    [HttpPut("accounting/{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> UpdateBusinessPartyAccounting(Guid id, UpdateBusinessPartyAccountingDto updateBusinessPartyAccountingDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var businessPartyAccounting = await _businessPartyService.UpdateBusinessPartyAccountingAsync(id, updateBusinessPartyAccountingDto, currentUser, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return NotFound(new { message = $"Business party accounting with ID {id} not found." });
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the business party accounting record.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a business party accounting record (soft delete).
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Business party accounting record deleted successfully</response>
    /// <response code="404">If the business party accounting record is not found</response>
    [HttpDelete("accounting/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBusinessPartyAccounting(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var deleted = await _businessPartyService.DeleteBusinessPartyAccountingAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Business party accounting with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the business party accounting record.", detail = ex.Message });
        }
    }

    #endregion
}