using Prym.DTOs.Store;
using Prym.Server.ModelBinders;
using Prym.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

/// <summary>
/// REST API controller for store user management with multi-tenant support.
/// Provides CRUD operations for store users within the authenticated user's tenant context.
/// Read operations are available to all authenticated users (for POS operator selection).
/// Write operations require Manager role.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class StoreUsersController(IStoreUserService storeUserService, ITenantContext tenantContext) : BaseApiController
{

    #region StoreUser Endpoints

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

    #endregion

    #region StoreUserGroup Endpoints

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

    #endregion

    #region StoreUserPrivilege Endpoints

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

    #endregion

    #region StoreUser Image Management - Issue #315

    /// <summary>
    /// Uploads a photo for a store user (with GDPR consent validation).
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="file">Photo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user with photo</returns>
    /// <response code="200">Photo uploaded successfully</response>
    /// <response code="400">If file is invalid or consent not given</response>
    /// <response code="404">If store user not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/photo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserDto>> UploadStoreUserPhoto(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file == null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            var updatedStoreUser = await storeUserService.UploadStoreUserPhotoAsync(id, file, cancellationToken);
            if (updatedStoreUser is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(updatedStoreUser);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("consent"))
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the photo.", ex);
        }
    }

    /// <summary>
    /// Gets the photo DocumentReference for a store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Photo document reference</returns>
    /// <response code="200">Returns the photo document</response>
    /// <response code="404">If store user not found or has no photo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}/photo")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserPhotoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var photoDocument = await storeUserService.GetStoreUserPhotoDocumentAsync(id, cancellationToken);
            if (photoDocument is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return Ok(photoDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the photo.", ex);
        }
    }

    /// <summary>
    /// Deletes the photo for a store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Photo deleted successfully</response>
    /// <response code="404">If store user not found or has no photo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}/photo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserPhoto(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStoreUserPhotoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the photo.", ex);
        }
    }

    #endregion

    #region StoreUserGroup Image Management - Issue #315

    /// <summary>
    /// Uploads a logo for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="file">Logo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user group with logo</returns>
    /// <response code="200">Logo uploaded successfully</response>
    /// <response code="400">If file is invalid</response>
    /// <response code="404">If store user group not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("groups/{id:guid}/logo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserGroupDto>> UploadStoreUserGroupLogo(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file == null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            var updatedGroup = await storeUserService.UploadStoreUserGroupLogoAsync(id, file, cancellationToken);
            if (updatedGroup is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(updatedGroup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the logo.", ex);
        }
    }

    /// <summary>
    /// Gets the logo DocumentReference for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logo document reference</returns>
    /// <response code="200">Returns the logo document</response>
    /// <response code="404">If store user group not found or has no logo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("groups/{id:guid}/logo")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserGroupLogoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var logoDocument = await storeUserService.GetStoreUserGroupLogoDocumentAsync(id, cancellationToken);
            if (logoDocument is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return Ok(logoDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the logo.", ex);
        }
    }

    /// <summary>
    /// Deletes the logo for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Logo deleted successfully</response>
    /// <response code="404">If store user group not found or has no logo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("groups/{id:guid}/logo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserGroupLogo(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStoreUserGroupLogoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the logo.", ex);
        }
    }

    #endregion

    #region StorePos Endpoints

    /// <summary>
    /// Gets all store POS terminals with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page and pageSize are automatically capped to configured limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store POS terminals</returns>
    /// <response code="200">Returns the paginated list of store POS terminals</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("pos")]
    [ProducesResponseType(typeof(PagedResult<StorePosDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StorePosDto>>> GetStorePoses(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await storeUserService.GetStorePosesAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store POS terminals.", ex);
        }
    }

    /// <summary>
    /// Gets a store POS by ID.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store POS details</returns>
    /// <response code="200">Returns the store POS</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpGet("pos/{id:guid}")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePosDto>> GetStorePos(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storePos = await storeUserService.GetStorePosByIdAsync(id, cancellationToken);

            if (storePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store POS.", ex);
        }
    }

    /// <summary>
    /// Creates a new store POS.
    /// </summary>
    /// <param name="createStorePosDto">Store POS creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store POS</returns>
    /// <response code="201">Returns the newly created store POS</response>
    /// <response code="400">If the store POS data is invalid</response>
    [HttpPost("pos")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorePosDto>> CreateStorePos(CreateStorePosDto createStorePosDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storePos = await storeUserService.CreateStorePosAsync(createStorePosDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStorePos), new { id = storePos.Id }, storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the store POS.", ex);
        }
    }

    /// <summary>
    /// Updates an existing store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="updateStorePosDto">Store POS update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store POS</returns>
    /// <response code="200">Returns the updated store POS</response>
    /// <response code="400">If the store POS data is invalid</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpPut("pos/{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePosDto>> UpdateStorePos(Guid id, UpdateStorePosDto updateStorePosDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storePos = await storeUserService.UpdateStorePosAsync(id, updateStorePosDto, currentUser, cancellationToken);

            if (storePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the store POS.", ex);
        }
    }

    /// <summary>
    /// Deletes a store POS (soft delete).
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Store POS deleted successfully</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpDelete("pos/{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorePos(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await storeUserService.DeleteStorePosAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the store POS.", ex);
        }
    }

    #endregion

    #region StorePos Image Management - Issue #315

    /// <summary>
    /// Uploads an image for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="file">Image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store POS with image</returns>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">If file is invalid</response>
    /// <response code="404">If store POS not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("pos/{id:guid}/image")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorePosDto>> UploadStorePosImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file == null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            var updatedStorePos = await storeUserService.UploadStorePosImageAsync(id, file, cancellationToken);
            if (updatedStorePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(updatedStorePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the image.", ex);
        }
    }

    /// <summary>
    /// Gets the image DocumentReference for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image document reference</returns>
    /// <response code="200">Returns the image document</response>
    /// <response code="404">If store POS not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("pos/{id:guid}/image")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStorePosImageDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var imageDocument = await storeUserService.GetStorePosImageDocumentAsync(id, cancellationToken);
            if (imageDocument is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return Ok(imageDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the image.", ex);
        }
    }

    /// <summary>
    /// Deletes the image for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Image deleted successfully</response>
    /// <response code="404">If store POS not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("pos/{id:guid}/image")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStorePosImage(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStorePosImageAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the image.", ex);
        }
    }

    #endregion
}