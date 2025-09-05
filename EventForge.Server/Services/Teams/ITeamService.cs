using EventForge.DTOs.Teams;
using EventForge.DTOs.Common;

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

    // Document Reference operations

    /// <summary>
    /// Gets all documents for a specific owner (Team or TeamMember).
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="ownerType">Owner type ("Team" or "TeamMember")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document references</returns>
    Task<IEnumerable<DocumentReferenceDto>> GetDocumentsByOwnerAsync(Guid ownerId, string ownerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document reference by ID.
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document reference DTO or null if not found</returns>
    Task<DocumentReferenceDto?> GetDocumentReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document reference.
    /// </summary>
    /// <param name="createDocumentDto">Document creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document reference DTO</returns>
    Task<DocumentReferenceDto> CreateDocumentReferenceAsync(CreateDocumentReferenceDto createDocumentDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document reference.
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="updateDocumentDto">Document update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document reference DTO or null if not found</returns>
    Task<DocumentReferenceDto?> UpdateDocumentReferenceAsync(Guid id, UpdateDocumentReferenceDto updateDocumentDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document reference (soft delete).
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Membership Card operations

    /// <summary>
    /// Gets all membership cards for a team member.
    /// </summary>
    /// <param name="teamMemberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of membership cards</returns>
    Task<IEnumerable<MembershipCardDto>> GetMembershipCardsByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a membership card by ID.
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Membership card DTO or null if not found</returns>
    Task<MembershipCardDto?> GetMembershipCardByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new membership card.
    /// </summary>
    /// <param name="createMembershipCardDto">Membership card creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created membership card DTO</returns>
    Task<MembershipCardDto> CreateMembershipCardAsync(CreateMembershipCardDto createMembershipCardDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing membership card.
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="updateMembershipCardDto">Membership card update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated membership card DTO or null if not found</returns>
    Task<MembershipCardDto?> UpdateMembershipCardAsync(Guid id, UpdateMembershipCardDto updateMembershipCardDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a membership card (soft delete).
    /// </summary>
    /// <param name="id">Membership card ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteMembershipCardAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Insurance Policy operations

    /// <summary>
    /// Gets all insurance policies for a team member.
    /// </summary>
    /// <param name="teamMemberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of insurance policies</returns>
    Task<IEnumerable<InsurancePolicyDto>> GetInsurancePoliciesByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an insurance policy by ID.
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Insurance policy DTO or null if not found</returns>
    Task<InsurancePolicyDto?> GetInsurancePolicyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new insurance policy.
    /// </summary>
    /// <param name="createInsurancePolicyDto">Insurance policy creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created insurance policy DTO</returns>
    Task<InsurancePolicyDto> CreateInsurancePolicyAsync(CreateInsurancePolicyDto createInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing insurance policy.
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="updateInsurancePolicyDto">Insurance policy update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated insurance policy DTO or null if not found</returns>
    Task<InsurancePolicyDto?> UpdateInsurancePolicyAsync(Guid id, UpdateInsurancePolicyDto updateInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an insurance policy (soft delete).
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteInsurancePolicyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Business Logic Methods

    /// <summary>
    /// Validates jersey number uniqueness within a team.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="jerseyNumber">Jersey number to validate</param>
    /// <param name="excludeTeamMemberId">Team member ID to exclude from validation (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if jersey number is available, false if already taken</returns>
    Task<bool> ValidateJerseyNumberAsync(Guid teamId, int jerseyNumber, Guid? excludeTeamMemberId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team members with expiring documents (certificates, etc.).
    /// </summary>
    /// <param name="daysBeforeExpiry">Number of days before expiry to consider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of team members with expiring documents</returns>
    Task<IEnumerable<TeamMemberDto>> GetMembersWithExpiringDocumentsAsync(int daysBeforeExpiry = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates team member eligibility based on documents and requirements.
    /// </summary>
    /// <param name="teamMemberId">Team member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Eligibility validation result</returns>
    Task<EligibilityValidationResult> ValidateTeamMemberEligibilityAsync(Guid teamMemberId, CancellationToken cancellationToken = default);
}