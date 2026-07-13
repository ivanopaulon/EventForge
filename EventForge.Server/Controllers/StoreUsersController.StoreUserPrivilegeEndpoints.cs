using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Gets all store user privileges with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page and pageSize are automatically capped to configured limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user privileges</returns>
    /// <response code="200">Returns the paginated list of store user privileges</response>
    [HttpGet("privileges")]
    [ProducesResponseType(typeof(PagedResult<StoreUserPrivilegeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StoreUserPrivilegeDto>>> GetStoreUserPrivileges(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await storeUserService.GetStoreUserPrivilegesAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store user privileges.", ex);
        }
    }

    /// <summary>
    /// Gets a store user privilege by ID.
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user privilege details</returns>
    /// <response code="200">Returns the store user privilege</response>
    /// <response code="404">If the store user privilege is not found</response>
    [HttpGet("privileges/{id:guid}")]
    [ProducesResponseType(typeof(StoreUserPrivilegeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserPrivilegeDto>> GetStoreUserPrivilege(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserPrivilege = await storeUserService.GetStoreUserPrivilegeByIdAsync(id, cancellationToken);

            if (storeUserPrivilege is null)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store user privilege.", ex);
        }
    }

    /// <summary>
    /// Gets store user privileges by group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of store user privileges for the group</returns>
    /// <response code="200">Returns the list of store user privileges</response>
    [HttpGet("privileges/by-group/{groupId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StoreUserPrivilegeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StoreUserPrivilegeDto>>> GetStoreUserPrivilegesByGroup(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserPrivileges = await storeUserService.GetStoreUserPrivilegesByGroupAsync(groupId, cancellationToken);
            return Ok(storeUserPrivileges);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store user privileges by group.", ex);
        }
    }

    /// <summary>
    /// Creates a new store user privilege.
    /// </summary>
    /// <param name="createStoreUserPrivilegeDto">Store user privilege creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user privilege</returns>
    /// <response code="201">Returns the newly created store user privilege</response>
    /// <response code="400">If the store user privilege data is invalid</response>
    [HttpPost("privileges")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserPrivilegeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreUserPrivilegeDto>> CreateStoreUserPrivilege(CreateStoreUserPrivilegeDto createStoreUserPrivilegeDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUserPrivilege = await storeUserService.CreateStoreUserPrivilegeAsync(createStoreUserPrivilegeDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserPrivilege), new { id = storeUserPrivilege.Id }, storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the store user privilege.", ex);
        }
    }

    /// <summary>
    /// Updates an existing store user privilege.
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="updateStoreUserPrivilegeDto">Store user privilege update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user privilege</returns>
    /// <response code="200">Returns the updated store user privilege</response>
    /// <response code="400">If the store user privilege data is invalid</response>
    /// <response code="404">If the store user privilege is not found</response>
    [HttpPut("privileges/{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserPrivilegeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserPrivilegeDto>> UpdateStoreUserPrivilege(Guid id, UpdateStoreUserPrivilegeDto updateStoreUserPrivilegeDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUserPrivilege = await storeUserService.UpdateStoreUserPrivilegeAsync(id, updateStoreUserPrivilegeDto, currentUser, cancellationToken);

            if (storeUserPrivilege is null)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the store user privilege.", ex);
        }
    }

    /// <summary>
    /// Deletes a store user privilege (soft delete).
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Store user privilege deleted successfully</response>
    /// <response code="404">If the store user privilege is not found</response>
    [HttpDelete("privileges/{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUserPrivilege(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await storeUserService.DeleteStoreUserPrivilegeAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the store user privilege.", ex);
        }
    }

}
