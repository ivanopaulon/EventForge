using EventForge.Server.Filters;
using EventForge.Server.Services.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Teams;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for team and team member management with multi-tenant support.
/// Provides comprehensive CRUD operations for teams and team members
/// within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[RequireLicenseFeature("BasicTeamManagement")]
public class TeamsController(
    ITeamService teamService,
    ITenantContext tenantContext) : BaseApiController
{

    #region Team CRUD Operations

    /// <summary>
    /// Gets all teams with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of teams</returns>
    /// <response code="200">Returns the paginated list of teams</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TeamDto>>> GetTeams(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError is not null) return paginationError;

        // Validate tenant access
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await teamService.GetTeamsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving teams.", ex);
        }
    }

    /// <summary>
    /// Gets all teams for a specific event.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of teams for the event</returns>
    /// <response code="200">Returns the list of teams for the event</response>
    [HttpGet("by-event/{eventId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TeamDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetTeamsByEvent(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await teamService.GetTeamsByEventAsync(eventId, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving teams for the event.", ex);
        }
    }

    /// <summary>
    /// Gets a team by ID.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team information</returns>
    /// <response code="200">Returns the team</response>
    /// <response code="404">If the team is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> GetTeam(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var team = await teamService.GetTeamByIdAsync(id, cancellationToken);

            if (team is null)
            {
                return CreateNotFoundProblem($"Team with ID {id} not found.");
            }

            return Ok(team);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the team.", ex);
        }
    }

    /// <summary>
    /// Gets detailed team information including members.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed team information</returns>
    /// <response code="200">Returns the detailed team information</response>
    /// <response code="404">If the team is not found</response>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(TeamDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDetailDto>> GetTeamDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var teamDetail = await teamService.GetTeamDetailAsync(id, cancellationToken);

            if (teamDetail is null)
            {
                return CreateNotFoundProblem($"Team with ID {id} not found.");
            }

            return Ok(teamDetail);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the team details.", ex);
        }
    }

    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="createTeamDto">Team creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created team</returns>
    /// <response code="201">Returns the newly created team</response>
    /// <response code="400">If the team data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamDto>> CreateTeam(
        [FromBody] CreateTeamDto createTeamDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var team = await teamService.CreateTeamAsync(createTeamDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTeam),
                new { id = team.Id },
                team);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the team.", ex);
        }
    }

    /// <summary>
    /// Updates an existing team.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="updateTeamDto">Team update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated team</returns>
    /// <response code="200">Returns the updated team</response>
    /// <response code="400">If the team data is invalid</response>
    /// <response code="404">If the team is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> UpdateTeam(
        Guid id,
        [FromBody] UpdateTeamDto updateTeamDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var team = await teamService.UpdateTeamAsync(id, updateTeamDto, currentUser, cancellationToken);

            if (team is null)
            {
                return CreateNotFoundProblem($"Team with ID {id} not found.");
            }

            return Ok(team);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the team.", ex);
        }
    }

    /// <summary>
    /// Deletes a team.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the team was successfully deleted</response>
    /// <response code="404">If the team is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await teamService.DeleteTeamAsync(id, currentUser, cancellationToken);

            if (!result)
            {
                return CreateNotFoundProblem($"Team with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the team.", ex);
        }
    }

    #endregion

    #region Team Member Operations

    /// <summary>
    /// Gets all members of a team.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of team members</returns>
    /// <response code="200">Returns the list of team members</response>
    [HttpGet("{teamId:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamMemberDto>>> GetTeamMembers(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await teamService.GetTeamMembersAsync(teamId, cancellationToken);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving team members.", ex);
        }
    }

    /// <summary>
    /// Gets all team members across all teams that have a date of birth set.
    /// Used for birthday tracking in the calendar scheduler.
    /// </summary>
    /// <returns>List of team members with a date of birth</returns>
    /// <response code="200">Returns the list of team members with birthdays</response>
    [HttpGet("members-with-birthdays")]
    [ProducesResponseType(typeof(IEnumerable<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamMemberDto>>> GetMembersWithBirthdays(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var members = await teamService.GetMembersWithBirthdayAsync(cancellationToken);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving members with birthdays.", ex);
        }
    }

    /// <summary>
    /// Gets a team member by ID.
    /// </summary>
    /// <param name="memberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team member information</returns>
    /// <response code="200">Returns the team member</response>
    /// <response code="404">If the team member is not found</response>
    [HttpGet("members/{memberId:guid}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberDto>> GetTeamMember(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var member = await teamService.GetTeamMemberByIdAsync(memberId, cancellationToken);

            if (member is null)
            {
                return CreateNotFoundProblem($"Team member with ID {memberId} not found.");
            }

            return Ok(member);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the team member.", ex);
        }
    }

    /// <summary>
    /// Adds a new member to a team.
    /// </summary>
    /// <param name="createTeamMemberDto">Team member creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created team member</returns>
    /// <response code="201">Returns the newly created team member</response>
    /// <response code="400">If the team member data is invalid</response>
    [HttpPost("members")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamMemberDto>> AddTeamMember(
        [FromBody] CreateTeamMemberDto createTeamMemberDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var member = await teamService.AddTeamMemberAsync(createTeamMemberDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTeamMember),
                new { memberId = member.Id },
                member);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the team member.", ex);
        }
    }

    /// <summary>
    /// Updates an existing team member.
    /// </summary>
    /// <param name="memberId">Team member ID</param>
    /// <param name="updateTeamMemberDto">Team member update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated team member</returns>
    /// <response code="200">Returns the updated team member</response>
    /// <response code="400">If the team member data is invalid</response>
    /// <response code="404">If the team member is not found</response>
    [HttpPut("members/{memberId:guid}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberDto>> UpdateTeamMember(
        Guid memberId,
        [FromBody] UpdateTeamMemberDto updateTeamMemberDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var member = await teamService.UpdateTeamMemberAsync(memberId, updateTeamMemberDto, currentUser, cancellationToken);

            if (member is null)
            {
                return CreateNotFoundProblem($"Team member with ID {memberId} not found.");
            }

            return Ok(member);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the team member.", ex);
        }
    }

    /// <summary>
    /// Removes a member from a team.
    /// </summary>
    /// <param name="memberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the team member was successfully removed</response>
    /// <response code="404">If the team member is not found</response>
    [HttpDelete("members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeamMember(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await teamService.RemoveTeamMemberAsync(memberId, currentUser, cancellationToken);

            if (!result)
            {
                return CreateNotFoundProblem($"Team member with ID {memberId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing the team member.", ex);
        }
    }

    /// <summary>
    /// Gets other active team members sharing the given fiscal code, in a team different from the
    /// excluded member's own team. This is an informational, non-blocking check: the client decides
    /// how to surface the warning (e.g. badge on the athlete card, indicator in the members list);
    /// the server never blocks saving based on this result.
    /// </summary>
    /// <param name="fiscalCode">Fiscal code to search for</param>
    /// <param name="excludeMemberId">Team member ID to exclude from the search (and whose team is excluded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of other active team members sharing the fiscal code (empty if no conflict)</returns>
    /// <response code="200">Returns the list of conflicting team members (possibly empty)</response>
    /// <response code="400">If the fiscal code is missing</response>
    [HttpGet("members/by-fiscal-code/{fiscalCode}/conflicts")]
    [ProducesResponseType(typeof(List<TeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TeamMemberDto>>> GetFiscalCodeConflicts(
        string fiscalCode,
        [FromQuery] Guid excludeMemberId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fiscalCode))
        {
            return CreateValidationProblemDetails("The fiscal code is required.");
        }

        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var conflicts = await teamService.GetOtherActiveTeamsForFiscalCodeAsync(fiscalCode, excludeMemberId, cancellationToken);
            return Ok(conflicts);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking fiscal code conflicts.", ex);
        }
    }

    #endregion

    #region Membership Card Operations

    /// <summary>
    /// Gets all membership cards for a specific team member.
    /// </summary>
    /// <param name="memberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of membership cards</returns>
    /// <response code="200">Returns the list of membership cards</response>
    [HttpGet("members/{memberId:guid}/membership-cards")]
    [ProducesResponseType(typeof(IEnumerable<MembershipCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MembershipCardDto>>> GetMembershipCards(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var cards = await teamService.GetMembershipCardsByMemberAsync(memberId, cancellationToken);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving membership cards.", ex);
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
    [HttpGet("membership-cards/{id:guid}")]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return CreateInternalServerErrorProblem("An error occurred while retrieving the membership card.", ex);
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
    [HttpPost("membership-cards")]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembershipCardDto>> CreateMembershipCard(
        [FromBody] CreateMembershipCardDto createCardDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var card = await teamService.CreateMembershipCardAsync(createCardDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetMembershipCard),
                new { id = card.Id },
                card);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the membership card.", ex);
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
    [HttpPut("membership-cards/{id:guid}")]
    [ProducesResponseType(typeof(MembershipCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipCardDto>> UpdateMembershipCard(
        Guid id,
        [FromBody] UpdateMembershipCardDto updateCardDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var card = await teamService.UpdateMembershipCardAsync(id, updateCardDto, currentUser, cancellationToken);

            if (card is null)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return Ok(card);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the membership card.", ex);
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
    [HttpDelete("membership-cards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMembershipCard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var deleted = await teamService.DeleteMembershipCardAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Membership card {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the membership card.", ex);
        }
    }

    #endregion

    #region Insurance Policy Operations

    /// <summary>
    /// Gets all insurance policies for a specific team member.
    /// </summary>
    /// <param name="memberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of insurance policies</returns>
    /// <response code="200">Returns the list of insurance policies</response>
    [HttpGet("members/{memberId:guid}/insurance-policies")]
    [ProducesResponseType(typeof(IEnumerable<InsurancePolicyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InsurancePolicyDto>>> GetInsurancePolicies(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var policies = await teamService.GetInsurancePoliciesByMemberAsync(memberId, cancellationToken);
            return Ok(policies);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving insurance policies.", ex);
        }
    }

    /// <summary>
    /// Gets a specific insurance policy by ID.
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Insurance policy details</returns>
    /// <response code="200">Returns the insurance policy</response>
    /// <response code="404">If the insurance policy is not found</response>
    [HttpGet("insurance-policies/{id:guid}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsurancePolicyDto>> GetInsurancePolicy(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var policy = await teamService.GetInsurancePolicyByIdAsync(id, cancellationToken);

            if (policy is null)
            {
                return CreateNotFoundProblem($"Insurance policy {id} not found");
            }

            return Ok(policy);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the insurance policy.", ex);
        }
    }

    /// <summary>
    /// Creates a new insurance policy.
    /// </summary>
    /// <param name="createPolicyDto">Insurance policy creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created insurance policy</returns>
    /// <response code="201">Returns the created insurance policy</response>
    /// <response code="400">If the request data is invalid</response>
    [HttpPost("insurance-policies")]
    [ProducesResponseType(typeof(InsurancePolicyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InsurancePolicyDto>> CreateInsurancePolicy(
        [FromBody] CreateInsurancePolicyDto createPolicyDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var policy = await teamService.CreateInsurancePolicyAsync(createPolicyDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetInsurancePolicy),
                new { id = policy.Id },
                policy);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the insurance policy.", ex);
        }
    }

    /// <summary>
    /// Updates an existing insurance policy.
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="updatePolicyDto">Insurance policy update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated insurance policy</returns>
    /// <response code="200">Returns the updated insurance policy</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the insurance policy is not found</response>
    [HttpPut("insurance-policies/{id:guid}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsurancePolicyDto>> UpdateInsurancePolicy(
        Guid id,
        [FromBody] UpdateInsurancePolicyDto updatePolicyDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var policy = await teamService.UpdateInsurancePolicyAsync(id, updatePolicyDto, currentUser, cancellationToken);

            if (policy is null)
            {
                return CreateNotFoundProblem($"Insurance policy {id} not found");
            }

            return Ok(policy);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the insurance policy.", ex);
        }
    }

    /// <summary>
    /// Deletes an insurance policy (soft delete).
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the insurance policy was deleted successfully</response>
    /// <response code="404">If the insurance policy is not found</response>
    [HttpDelete("insurance-policies/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInsurancePolicy(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = GetCurrentUser();
            var deleted = await teamService.DeleteInsurancePolicyAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Insurance policy {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the insurance policy.", ex);
        }
    }

    #endregion

}