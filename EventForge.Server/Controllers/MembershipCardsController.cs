using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Teams;
using EventForge.Server.Services.Teams;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for managing team member membership cards.
/// Handles CRUD operations for federation membership cards with validity tracking.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class MembershipCardsController : BaseApiController
{
    private readonly ITeamService _teamService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MembershipCardsController> _logger;

    public MembershipCardsController(
        ITeamService teamService,
        ITenantContext tenantContext,
        ILogger<MembershipCardsController> logger)
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all membership cards for a specific team member.
    /// </summary>
    /// <param name="teamMemberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of membership cards</returns>
    /// <response code="200">Returns the list of membership cards</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("member/{teamMemberId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<MembershipCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MembershipCardDto>>> GetMembershipCardsByMember(
        Guid teamMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            var cards = await _teamService.GetMembershipCardsByMemberAsync(teamMemberId, cancellationToken);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership cards for team member {TeamMemberId}", teamMemberId);
            return CreateInternalServerErrorProblem("Error retrieving membership cards", ex);
        }
    }

    /// <summary>
    /// Gets a specific membership card by ID.
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Membership card details</returns>
    /// <response code="200">Returns the membership card</response>
    /// <response code="404">If the membership card is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MembershipCardDto>> GetMembershipCard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            var card = await _teamService.GetMembershipCardByIdAsync(id, cancellationToken);

            if (card == null)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return Ok(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership card {CardId}", id);
            return CreateInternalServerErrorProblem("Error retrieving membership card", ex);
        }
    }

    /// <summary>
    /// Creates a new membership card.
    /// </summary>
    /// <param name="createCardDto">Membership card creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created membership card</returns>
    /// <response code="201">Returns the created membership card</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MembershipCardDto>> CreateMembershipCard(
        [FromBody] CreateMembershipCardDto createCardDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var card = await _teamService.CreateMembershipCardAsync(createCardDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetMembershipCard),
                new { id = card.Id },
                card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating membership card");
            return CreateInternalServerErrorProblem("Error creating membership card", ex);
        }
    }

    /// <summary>
    /// Updates an existing membership card.
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="updateCardDto">Membership card update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated membership card</returns>
    /// <response code="200">Returns the updated membership card</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the membership card is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MembershipCardDto>> UpdateMembershipCard(
        Guid id,
        [FromBody] UpdateMembershipCardDto updateCardDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var card = await _teamService.UpdateMembershipCardAsync(id, updateCardDto, currentUser, cancellationToken);

            if (card == null)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return Ok(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership card {CardId}", id);
            return CreateInternalServerErrorProblem("Error updating membership card", ex);
        }
    }

    /// <summary>
    /// Deletes a membership card (soft delete).
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the membership card was deleted successfully</response>
    /// <response code="404">If the membership card is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMembershipCard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var deleted = await _teamService.DeleteMembershipCardAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting membership card {CardId}", id);
            return CreateInternalServerErrorProblem("Error deleting membership card", ex);
        }
    }
}