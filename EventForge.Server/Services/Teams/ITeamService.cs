using EventForge.Server.DTOs.Teams;

namespace EventForge.Server.Services.Teams;

/// <summary>
/// Service interface for managing teams and team members.
/// </summary>
public interface ITeamService
{
    // Team CRUD operations

    /// <summary>
    /// Gets all teams with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of teams</returns>
    Task<PagedResult<TeamDto>> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all teams for a specific event.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of teams for the event</returns>
    Task<IEnumerable<TeamDto>> GetTeamsByEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a team by ID.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team DTO or null if not found</returns>
    Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed team information including members.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed team DTO or null if not found</returns>
    Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="createTeamDto">Team creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created team DTO</returns>
    Task<TeamDto> CreateTeamAsync(CreateTeamDto createTeamDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing team.
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="updateTeamDto">Team update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated team DTO or null if not found</returns>
    Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto updateTeamDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a team (soft delete).
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteTeamAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Team Member management operations

    /// <summary>
    /// Gets all members of a team.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of team members</returns>
    Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a team member by ID.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team member DTO or null if not found</returns>
    Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new member to a team.
    /// </summary>
    /// <param name="createTeamMemberDto">Team member creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created team member DTO</returns>
    Task<TeamMemberDto> AddTeamMemberAsync(CreateTeamMemberDto createTeamMemberDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing team member.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="updateTeamMemberDto">Team member update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated team member DTO or null if not found</returns>
    Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberDto updateTeamMemberDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a member from a team (soft delete).
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveTeamMemberAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a team exists.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
}