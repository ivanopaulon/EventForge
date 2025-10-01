using EventForge.DTOs.Store;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for store user management with multi-tenant support.
/// Provides CRUD operations for store users within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class StoreUsersController : BaseApiController
{
    private readonly IStoreUserService _storeUserService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StoreUsersController> _logger;

    public StoreUsersController(IStoreUserService storeUserService, ITenantContext tenantContext, ILogger<StoreUsersController> logger)
    {
        _storeUserService = storeUserService ?? throw new ArgumentNullException(nameof(storeUserService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region StoreUser Endpoints

    /// <summary>
    /// Gets all store users with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store users</returns>
    /// <response code="200">Returns the paginated list of store users</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StoreUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StoreUserDto>>> GetStoreUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _storeUserService.GetStoreUsersAsync(page, pageSize, cancellationToken);
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
            var storeUser = await _storeUserService.GetStoreUserByIdAsync(id, cancellationToken);

            if (storeUser == null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user with ID {UserId}", id);
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
            var storeUsers = await _storeUserService.GetStoreUsersByGroupAsync(groupId, cancellationToken);
            return Ok(storeUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store users by group {GroupId}", groupId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving store users by group.", ex);
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
            var storeUser = await _storeUserService.CreateStoreUserAsync(createStoreUserDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUser), new { id = storeUser.Id }, storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user with data {@CreateStoreUserDto}", createStoreUserDto);
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
            var storeUser = await _storeUserService.UpdateStoreUserAsync(id, updateStoreUserDto, currentUser, cancellationToken);

            if (storeUser == null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user {UserId} with data {@UpdateStoreUserDto}", id, updateStoreUserDto);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUser(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _storeUserService.DeleteStoreUserAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user {UserId}", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the store user.", ex);
        }
    }

    #endregion

    #region StoreUserGroup Endpoints

    /// <summary>
    /// Gets all store user groups with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user groups</returns>
    /// <response code="200">Returns the paginated list of store user groups</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(PagedResult<StoreUserGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StoreUserGroupDto>>> GetStoreUserGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        try
        {
            var result = await _storeUserService.GetStoreUserGroupsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user groups with pagination (page: {Page}, pageSize: {PageSize})", page, pageSize);
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
            var storeUserGroup = await _storeUserService.GetStoreUserGroupByIdAsync(id, cancellationToken);

            if (storeUserGroup == null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user group {GroupId}", id);
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
            var storeUserGroup = await _storeUserService.CreateStoreUserGroupAsync(createStoreUserGroupDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserGroup), new { id = storeUserGroup.Id }, storeUserGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user group with data {@CreateStoreUserGroupDto}", createStoreUserGroupDto);
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
            var storeUserGroup = await _storeUserService.UpdateStoreUserGroupAsync(id, updateStoreUserGroupDto, currentUser, cancellationToken);

            if (storeUserGroup == null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user group {GroupId} with data {@UpdateStoreUserGroupDto}", id, updateStoreUserGroupDto);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUserGroup(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _storeUserService.DeleteStoreUserGroupAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user group {GroupId}", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the store user group.", ex);
        }
    }

    #endregion

    #region StoreUserPrivilege Endpoints

    /// <summary>
    /// Gets all store user privileges with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user privileges</returns>
    /// <response code="200">Returns the paginated list of store user privileges</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet("privileges")]
    [ProducesResponseType(typeof(PagedResult<StoreUserPrivilegeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StoreUserPrivilegeDto>>> GetStoreUserPrivileges(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        try
        {
            var result = await _storeUserService.GetStoreUserPrivilegesAsync(page, pageSize, cancellationToken);
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
            var storeUserPrivilege = await _storeUserService.GetStoreUserPrivilegeByIdAsync(id, cancellationToken);

            if (storeUserPrivilege == null)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user privilege {PrivilegeId}", id);
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
            var storeUserPrivileges = await _storeUserService.GetStoreUserPrivilegesByGroupAsync(groupId, cancellationToken);
            return Ok(storeUserPrivileges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store user privileges by group {GroupId}", groupId);
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
            var storeUserPrivilege = await _storeUserService.CreateStoreUserPrivilegeAsync(createStoreUserPrivilegeDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserPrivilege), new { id = storeUserPrivilege.Id }, storeUserPrivilege);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user privilege with data {@CreateStoreUserPrivilegeDto}", createStoreUserPrivilegeDto);
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
            var storeUserPrivilege = await _storeUserService.UpdateStoreUserPrivilegeAsync(id, updateStoreUserPrivilegeDto, currentUser, cancellationToken);

            if (storeUserPrivilege == null)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user privilege {PrivilegeId} with data {@UpdateStoreUserPrivilegeDto}", id, updateStoreUserPrivilegeDto);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStoreUserPrivilege(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _storeUserService.DeleteStoreUserPrivilegeAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user privilege with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user privilege {PrivilegeId}", id);
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
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserDto>> UploadStoreUserPhoto(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

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
            var updatedStoreUser = await _storeUserService.UploadStoreUserPhotoAsync(id, file, cancellationToken);
            if (updatedStoreUser == null)
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
            _logger.LogError(ex, "An error occurred while uploading photo for store user {StoreUserId}.", id);
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
    [ProducesResponseType(typeof(EventForge.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserPhotoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var photoDocument = await _storeUserService.GetStoreUserPhotoDocumentAsync(id, cancellationToken);
            if (photoDocument == null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return Ok(photoDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving photo for store user {StoreUserId}.", id);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserPhoto(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var deleted = await _storeUserService.DeleteStoreUserPhotoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting photo for store user {StoreUserId}.", id);
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
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserGroupDto>> UploadStoreUserGroupLogo(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

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
            var updatedGroup = await _storeUserService.UploadStoreUserGroupLogoAsync(id, file, cancellationToken);
            if (updatedGroup == null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(updatedGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while uploading logo for store user group {GroupId}.", id);
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
    [ProducesResponseType(typeof(EventForge.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserGroupLogoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var logoDocument = await _storeUserService.GetStoreUserGroupLogoDocumentAsync(id, cancellationToken);
            if (logoDocument == null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return Ok(logoDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving logo for store user group {GroupId}.", id);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserGroupLogo(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var deleted = await _storeUserService.DeleteStoreUserGroupLogoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting logo for store user group {GroupId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the logo.", ex);
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
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorePosDto>> UploadStorePosImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

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
            var updatedStorePos = await _storeUserService.UploadStorePosImageAsync(id, file, cancellationToken);
            if (updatedStorePos == null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(updatedStorePos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while uploading image for store POS {StorePosId}.", id);
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
    [ProducesResponseType(typeof(EventForge.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStorePosImageDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var imageDocument = await _storeUserService.GetStorePosImageDocumentAsync(id, cancellationToken);
            if (imageDocument == null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return Ok(imageDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving image for store POS {StorePosId}.", id);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStorePosImage(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var deleted = await _storeUserService.DeleteStorePosImageAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting image for store POS {StorePosId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the image.", ex);
        }
    }

    #endregion
}