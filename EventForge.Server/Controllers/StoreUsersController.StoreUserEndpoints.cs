using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Gets all store users with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page and pageSize are automatically capped to configured limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store users</returns>
    /// <response code="200">Returns the paginated list of store users</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StoreUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StoreUserDto>>> GetStoreUsers(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await storeUserService.GetStoreUsersAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store users.", ex);
        }
    }

    /// <summary>
    /// Gets a store user by ID.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user details</returns>
    /// <response code="200">Returns the store user</response>
    /// <response code="404">If the store user is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserDto>> GetStoreUser(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUser = await storeUserService.GetStoreUserByIdAsync(id, cancellationToken);

            if (storeUser is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store user.", ex);
        }
    }

    /// <summary>
    /// Gets a store user by username.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user details</returns>
    /// <response code="200">Returns the store user</response>
    /// <response code="404">If the store user is not found</response>
    [HttpGet("by-username/{username}")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserDto>> GetStoreUserByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUser = await storeUserService.GetStoreUserByUsernameAsync(username, cancellationToken);

            if (storeUser is null)
            {
                return CreateNotFoundProblem($"Store user with username {username} not found.");
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store user.", ex);
        }
    }

    /// <summary>
    /// Gets store users by group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of store users in the group</returns>
    /// <response code="200">Returns the list of store users</response>
    [HttpGet("by-group/{groupId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StoreUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StoreUserDto>>> GetStoreUsersByGroup(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUsers = await storeUserService.GetStoreUsersByGroupAsync(groupId, cancellationToken);
            return Ok(storeUsers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store users by group.", ex);
        }
    }

    /// <summary>
    /// Gets all store operators that have a date of birth set.
    /// Used for birthday tracking in the calendar scheduler.
    /// </summary>
    /// <returns>List of store users with a date of birth</returns>
    /// <response code="200">Returns the list of store users with birthdays</response>
    [HttpGet("with-birthdays")]
    [ProducesResponseType(typeof(IEnumerable<StoreUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StoreUserDto>>> GetStoreUsersWithBirthdays(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storeUsers = await storeUserService.GetStoreUsersWithBirthdayAsync(cancellationToken);
            return Ok(storeUsers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store users with birthdays.", ex);
        }
    }

    /// <summary>
    /// Creates a new store user.
    /// </summary>
    /// <param name="createStoreUserDto">Store user creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user</returns>
    /// <response code="201">Returns the newly created store user</response>
    /// <response code="400">If the store user data is invalid</response>
    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreUserDto>> CreateStoreUser(CreateStoreUserDto createStoreUserDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUser = await storeUserService.CreateStoreUserAsync(createStoreUserDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUser), new { id = storeUser.Id }, storeUser);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the store user.", ex);
        }
    }

    /// <summary>
    /// Updates an existing store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="updateStoreUserDto">Store user update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user</returns>
    /// <response code="200">Returns the updated store user</response>
    /// <response code="400">If the store user data is invalid</response>
    /// <response code="404">If the store user is not found</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserDto>> UpdateStoreUser(Guid id, UpdateStoreUserDto updateStoreUserDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storeUser = await storeUserService.UpdateStoreUserAsync(id, updateStoreUserDto, currentUser, cancellationToken);

            if (storeUser is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the store user.", ex);
        }
    }

    /// <summary>
    /// Deletes a store user (soft delete).
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Store user deleted successfully</response>
    /// <response code="404">If the store user is not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUser(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await storeUserService.DeleteStoreUserAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the store user.", ex);
        }
    }

    /// <summary>
    /// Sets or resets the quick PIN for a store user.
    /// </summary>
    [HttpPost("/api/v1/store-users/{id:guid}/pin")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPin(Guid id, [FromBody] StoreUserPinDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            await storeUserService.SetPinAsync(id, dto.Pin, GetCurrentUser(), cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while setting the quick PIN.", ex);
        }
    }

    /// <summary>
    /// Validates the quick PIN for a store user.
    /// </summary>
    [HttpPost("/api/v1/store-users/{id:guid}/validate-pin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> ValidatePin(Guid id, [FromBody] StoreUserPinDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var isValid = await storeUserService.ValidatePinAsync(id, dto.Pin, cancellationToken);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while validating the quick PIN.", ex);
        }
    }

}
