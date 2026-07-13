using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Gets all store user groups with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page and pageSize are automatically capped to configured limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user groups</returns>
    /// <response code="200">Returns the paginated list of store user groups</response>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(PagedResult<StoreUserGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StoreUserGroupDto>>> GetStoreUserGroups(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await storeUserService.GetStoreUserGroupsAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store user groups.", ex);
        }
    }

    /// <summary>
    /// Gets a store user group by ID.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user group details</returns>
    /// <response code="200">Returns the store user group</response>
    /// <response code="404">If the store user group is not found</response>
    [HttpGet("groups/{id:guid}")]
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserGroupDto>> GetStoreUserGroup(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUserGroup = await storeUserService.GetStoreUserGroupByIdAsync(id, cancellationToken);

            if (storeUserGroup is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store user group.", ex);
        }
    }

    /// <summary>
    /// Creates a new store user group.
    /// </summary>
    /// <param name="createStoreUserGroupDto">Store user group creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user group</returns>
    /// <response code="201">Returns the newly created store user group</response>
    /// <response code="400">If the store user group data is invalid</response>
    [HttpPost("groups")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreUserGroupDto>> CreateStoreUserGroup(CreateStoreUserGroupDto createStoreUserGroupDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUserGroup = await storeUserService.CreateStoreUserGroupAsync(createStoreUserGroupDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserGroup), new { id = storeUserGroup.Id }, storeUserGroup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the store user group.", ex);
        }
    }

    /// <summary>
    /// Updates an existing store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="updateStoreUserGroupDto">Store user group update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user group</returns>
    /// <response code="200">Returns the updated store user group</response>
    /// <response code="400">If the store user group data is invalid</response>
    /// <response code="404">If the store user group is not found</response>
    [HttpPut("groups/{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserGroupDto>> UpdateStoreUserGroup(Guid id, UpdateStoreUserGroupDto updateStoreUserGroupDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUserGroup = await storeUserService.UpdateStoreUserGroupAsync(id, updateStoreUserGroupDto, currentUser, cancellationToken);

            if (storeUserGroup is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the store user group.", ex);
        }
    }

    /// <summary>
    /// Deletes a store user group (soft delete).
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Store user group deleted successfully</response>
    /// <response code="404">If the store user group is not found</response>
    [HttpDelete("groups/{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUserGroup(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await storeUserService.DeleteStoreUserGroupAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the store user group.", ex);
        }
    }

}
