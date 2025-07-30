using EventForge.DTOs.Teams;
using EventForge.Server.Services.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for team and team member management.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class TeamsController : BaseApiController
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
    }

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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TeamDto>>> GetTeams(
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
            var result = await _teamService.GetTeamsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving teams.", error = ex.Message });
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
            var teams = await _teamService.GetTeamsByEventAsync(eventId, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving teams for the event.", error = ex.Message });
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
            var team = await _teamService.GetTeamByIdAsync(id, cancellationToken);

            if (team == null)
            {
                return NotFound(new { message = $"Team with ID {id} not found." });
            }

            return Ok(team);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the team.", error = ex.Message });
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
            var teamDetail = await _teamService.GetTeamDetailAsync(id, cancellationToken);

            if (teamDetail == null)
            {
                return NotFound(new { message = $"Team with ID {id} not found." });
            }

            return Ok(teamDetail);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the team details.", error = ex.Message });
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
            var team = await _teamService.CreateTeamAsync(createTeamDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTeam),
                new { id = team.Id },
                team);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the team.", error = ex.Message });
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
            var team = await _teamService.UpdateTeamAsync(id, updateTeamDto, currentUser, cancellationToken);

            if (team == null)
            {
                return NotFound(new { message = $"Team with ID {id} not found." });
            }

            return Ok(team);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the team.", error = ex.Message });
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
            var result = await _teamService.DeleteTeamAsync(id, currentUser, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = $"Team with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the team.", error = ex.Message });
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
            var members = await _teamService.GetTeamMembersAsync(teamId, cancellationToken);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving team members.", error = ex.Message });
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
            var member = await _teamService.GetTeamMemberByIdAsync(memberId, cancellationToken);

            if (member == null)
            {
                return NotFound(new { message = $"Team member with ID {memberId} not found." });
            }

            return Ok(member);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the team member.", error = ex.Message });
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
            var member = await _teamService.AddTeamMemberAsync(createTeamMemberDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTeamMember),
                new { memberId = member.Id },
                member);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while adding the team member.", error = ex.Message });
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
            var member = await _teamService.UpdateTeamMemberAsync(memberId, updateTeamMemberDto, currentUser, cancellationToken);

            if (member == null)
            {
                return NotFound(new { message = $"Team member with ID {memberId} not found." });
            }

            return Ok(member);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the team member.", error = ex.Message });
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
            var result = await _teamService.RemoveTeamMemberAsync(memberId, currentUser, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = $"Team member with ID {memberId} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while removing the team member.", error = ex.Message });
        }
    }

    #endregion

}