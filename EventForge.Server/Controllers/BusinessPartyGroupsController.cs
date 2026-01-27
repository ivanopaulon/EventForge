using EventForge.DTOs.Business;
using EventForge.Server.Filters;
using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for Business Party Groups management with multi-tenant support.
/// Provides comprehensive CRUD operations for business party groups and their members.
/// </summary>
[Route("api/v1/business-party-groups")]
[Authorize]
[RequireLicenseFeature("BasicReporting")]
public class BusinessPartyGroupsController : BaseApiController
{
    private readonly IBusinessPartyGroupService _businessPartyGroupService;
    private readonly ITenantContext _tenantContext;

    public BusinessPartyGroupsController(
        IBusinessPartyGroupService businessPartyGroupService,
        ITenantContext tenantContext)
    {
        _businessPartyGroupService = businessPartyGroupService ?? throw new ArgumentNullException(nameof(businessPartyGroupService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Group Endpoints

    /// <summary>
    /// Gets all business party groups with optional pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="groupType">Optional group type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party groups</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyGroupDto>>> GetGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DTOs.Common.BusinessPartyGroupType? groupType = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyGroupService.GetGroupsAsync(page, pageSize, groupType, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business party groups.", ex);
        }
    }

    /// <summary>
    /// Gets a business party group by ID.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party group details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyGroupDto>> GetGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var group = await _businessPartyGroupService.GetGroupByIdAsync(id, cancellationToken);

            if (group == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Business Party Group not found",
                    Detail = $"No business party group found with ID {id}"
                });
            }

            return Ok(group);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving business party group {id}.", ex);
        }
    }

    /// <summary>
    /// Creates a new business party group.
    /// </summary>
    /// <param name="createDto">Group creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party group</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BusinessPartyGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyGroupDto>> CreateGroup(
        [FromBody] CreateBusinessPartyGroupDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (createDto == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Request body cannot be null"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var group = await _businessPartyGroupService.CreateGroupAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the business party group.", ex);
        }
    }

    /// <summary>
    /// Updates an existing business party group.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="updateDto">Group update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party group</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyGroupDto>> UpdateGroup(
        Guid id,
        [FromBody] UpdateBusinessPartyGroupDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (updateDto == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Request body cannot be null"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var group = await _businessPartyGroupService.UpdateGroupAsync(id, updateDto, currentUser, cancellationToken);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Business Party Group not found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while updating business party group {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes a business party group (soft delete).
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _businessPartyGroupService.DeleteGroupAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Business Party Group not found",
                    Detail = $"No business party group found with ID {id}"
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while deleting business party group {id}.", ex);
        }
    }

    #endregion

    #region Member Management Endpoints

    /// <summary>
    /// Gets all members of a specific business party group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of group members</returns>
    [HttpGet("{groupId:guid}/members")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyGroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyGroupMemberDto>>> GetGroupMembers(
        Guid groupId,
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
            var result = await _businessPartyGroupService.GetGroupMembersAsync(groupId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving members for group {groupId}.", ex);
        }
    }

    /// <summary>
    /// Adds a business party to a group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="addDto">Member addition details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created membership</returns>
    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType(typeof(BusinessPartyGroupMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyGroupMemberDto>> AddMemberToGroup(
        Guid groupId,
        [FromBody] AddBusinessPartyToGroupDto addDto,
        CancellationToken cancellationToken = default)
    {
        if (addDto == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Request body cannot be null"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var member = await _businessPartyGroupService.AddMemberToGroupAsync(groupId, addDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetGroupMembers), new { groupId }, member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while adding member to group {groupId}.", ex);
        }
    }

    /// <summary>
    /// Bulk adds multiple business parties to a group.
    /// </summary>
    /// <param name="bulkDto">Bulk addition details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("bulk-add-members")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkAddMembers(
        [FromBody] BulkAddMembersDto bulkDto,
        CancellationToken cancellationToken = default)
    {
        if (bulkDto == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Request body cannot be null"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var result = await _businessPartyGroupService.BulkAddMembersAsync(bulkDto, currentUser, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while bulk adding members.", ex);
        }
    }

    /// <summary>
    /// Removes a business party from a group (soft delete).
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="businessPartyId">Business Party ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{groupId:guid}/members/{businessPartyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMemberFromGroup(
        Guid groupId,
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var removed = await _businessPartyGroupService.RemoveMemberFromGroupAsync(groupId, businessPartyId, currentUser, cancellationToken);

            if (!removed)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Member not found",
                    Detail = $"Business Party {businessPartyId} is not a member of group {groupId}"
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while removing member from group {groupId}.", ex);
        }
    }

    /// <summary>
    /// Updates a membership.
    /// </summary>
    /// <param name="membershipId">Membership ID</param>
    /// <param name="updateDto">Membership update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated membership</returns>
    [HttpPut("members/{membershipId:guid}")]
    [ProducesResponseType(typeof(BusinessPartyGroupMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyGroupMemberDto>> UpdateMembership(
        Guid membershipId,
        [FromBody] UpdateBusinessPartyGroupMemberDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (updateDto == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Request body cannot be null"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var member = await _businessPartyGroupService.UpdateMembershipAsync(membershipId, updateDto, currentUser, cancellationToken);
            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Membership not found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while updating membership {membershipId}.", ex);
        }
    }

    #endregion

    #region Query Helper Endpoints

    /// <summary>
    /// Gets all groups for a specific business party.
    /// </summary>
    /// <param name="businessPartyId">Business Party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of groups</returns>
    [HttpGet("for-business-party/{businessPartyId:guid}")]
    [ProducesResponseType(typeof(List<BusinessPartyGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<BusinessPartyGroupDto>>> GetGroupsForBusinessParty(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var groups = await _businessPartyGroupService.GetGroupsForBusinessPartyAsync(businessPartyId, cancellationToken);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving groups for business party {businessPartyId}.", ex);
        }
    }

    #endregion
}
