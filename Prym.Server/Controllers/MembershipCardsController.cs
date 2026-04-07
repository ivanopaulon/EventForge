using Prym.DTOs.Teams;
using Prym.Server.Services.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

/// <summary>
/// REST API controller for managing team member membership cards.
/// Handles CRUD operations for federation membership cards with validity tracking.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class MembershipCardsController(
    ITeamService teamService,
    ITenantContext tenantContext) : BaseApiController
{

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
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var cards = await teamService.GetMembershipCardsByMemberAsync(teamMemberId, cancellationToken);
            return Ok(cards);
        }
        catch (Exception ex)
        {
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
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var card = await teamService.GetMembershipCardByIdAsync(id, cancellationToken);

            if (card is null)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return Ok(card);
        }
        catch (Exception ex)
        {
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
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var card = await teamService.CreateMembershipCardAsync(createCardDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetMembershipCard),
                new { id = card.Id },
                card);
        }
        catch (Exception ex)
        {
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
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var card = await teamService.UpdateMembershipCardAsync(id, updateCardDto, currentUser, cancellationToken);

            if (card is null)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return Ok(card);
        }
        catch (Exception ex)
        {
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
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var deleted = await teamService.DeleteMembershipCardAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting membership card", ex);
        }
    }
}