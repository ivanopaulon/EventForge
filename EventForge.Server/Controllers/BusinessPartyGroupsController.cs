using Prym.DTOs.Business;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
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
public class BusinessPartyGroupsController(
    IBusinessPartyGroupService businessPartyGroupService,
    ITenantContext tenantContext) : BaseApiController
{

    #region Group Endpoints

    /// <summary>
    /// Gets all business party groups with optional pagination and filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="groupType">Optional group type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party groups</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyGroupDto>>> GetGroups(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] BusinessPartyGroupType? groupType = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await businessPartyGroupService.GetGroupsAsync(pagination.Page, pagination.PageSize, groupType, cancellationToken);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var group = await businessPartyGroupService.GetGroupByIdAsync(id, cancellationToken);

            if (group is null)
            {
                return CreateNotFoundProblem($"No business party group found with ID {id}");
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
        if (createDto is null)
        {
            return CreateValidationProblemDetails("Request body cannot be null.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var group = await businessPartyGroupService.CreateGroupAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
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
        if (updateDto is null)
        {
            return CreateValidationProblemDetails("Request body cannot be null.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var group = await businessPartyGroupService.UpdateGroupAsync(id, updateDto, currentUser, cancellationToken);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return CreateNotFoundProblem(ex.Message);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await businessPartyGroupService.DeleteGroupAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"No business party group found with ID {id}");
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
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of group members</returns>
    [HttpGet("{groupId:guid}/members")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyGroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyGroupMemberDto>>> GetGroupMembers(
        Guid groupId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await businessPartyGroupService.GetGroupMembersAsync(groupId, pagination.Page, pagination.PageSize, cancellationToken);
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
        if (addDto is null)
        {
            return CreateValidationProblemDetails("Request body cannot be null.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var member = await businessPartyGroupService.AddMemberToGroupAsync(groupId, addDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetGroupMembers), new { groupId }, member);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
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
        if (bulkDto is null)
        {
            return CreateValidationProblemDetails("Request body cannot be null.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var result = await businessPartyGroupService.BulkAddMembersAsync(bulkDto, currentUser, cancellationToken);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var removed = await businessPartyGroupService.RemoveMemberFromGroupAsync(groupId, businessPartyId, currentUser, cancellationToken);

            if (!removed)
            {
                return CreateNotFoundProblem($"Business Party {businessPartyId} is not a member of group {groupId}");
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
        if (updateDto is null)
        {
            return CreateValidationProblemDetails("Request body cannot be null.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var member = await businessPartyGroupService.UpdateMembershipAsync(membershipId, updateDto, currentUser, cancellationToken);
            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return CreateNotFoundProblem(ex.Message);
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
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var groups = await businessPartyGroupService.GetGroupsForBusinessPartyAsync(businessPartyId, cancellationToken);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving groups for business party {businessPartyId}.", ex);
        }
    }

    #endregion
}
