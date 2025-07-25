using EventForge.DTOs.Store;
using EventForge.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for store user management.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class StoreUsersController : BaseApiController
{
    private readonly IStoreUserService _storeUserService;

    public StoreUsersController(IStoreUserService storeUserService)
    {
        _storeUserService = storeUserService ?? throw new ArgumentNullException(nameof(storeUserService));
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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StoreUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StoreUserDto>>> GetStoreUsers(
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
            var result = await _storeUserService.GetStoreUsersAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving store users.", detail = ex.Message });
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
                return NotFound(new { message = $"Store user with ID {id} not found." });
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the store user.", detail = ex.Message });
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
            return StatusCode(500, new { message = "An error occurred while retrieving store users by group.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUser = await _storeUserService.CreateStoreUserAsync(createStoreUserDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUser), new { id = storeUser.Id }, storeUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the store user.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUser = await _storeUserService.UpdateStoreUserAsync(id, updateStoreUserDto, currentUser, cancellationToken);

            if (storeUser == null)
            {
                return NotFound(new { message = $"Store user with ID {id} not found." });
            }

            return Ok(storeUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the store user.", detail = ex.Message });
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
            var currentUser = "system"; // TODO: Get from authentication context
            var deleted = await _storeUserService.DeleteStoreUserAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Store user with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the store user.", detail = ex.Message });
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
            var result = await _storeUserService.GetStoreUserGroupsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving store user groups.", detail = ex.Message });
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
                return NotFound(new { message = $"Store user group with ID {id} not found." });
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the store user group.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUserGroup = await _storeUserService.CreateStoreUserGroupAsync(createStoreUserGroupDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserGroup), new { id = storeUserGroup.Id }, storeUserGroup);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the store user group.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUserGroup = await _storeUserService.UpdateStoreUserGroupAsync(id, updateStoreUserGroupDto, currentUser, cancellationToken);

            if (storeUserGroup == null)
            {
                return NotFound(new { message = $"Store user group with ID {id} not found." });
            }

            return Ok(storeUserGroup);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the store user group.", detail = ex.Message });
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
            var currentUser = "system"; // TODO: Get from authentication context
            var deleted = await _storeUserService.DeleteStoreUserGroupAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Store user group with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the store user group.", detail = ex.Message });
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
            var result = await _storeUserService.GetStoreUserPrivilegesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving store user privileges.", detail = ex.Message });
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
                return NotFound(new { message = $"Store user privilege with ID {id} not found." });
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the store user privilege.", detail = ex.Message });
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
            return StatusCode(500, new { message = "An error occurred while retrieving store user privileges by group.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUserPrivilege = await _storeUserService.CreateStoreUserPrivilegeAsync(createStoreUserPrivilegeDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStoreUserPrivilege), new { id = storeUserPrivilege.Id }, storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the store user privilege.", detail = ex.Message });
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
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = "system"; // TODO: Get from authentication context
            var storeUserPrivilege = await _storeUserService.UpdateStoreUserPrivilegeAsync(id, updateStoreUserPrivilegeDto, currentUser, cancellationToken);

            if (storeUserPrivilege == null)
            {
                return NotFound(new { message = $"Store user privilege with ID {id} not found." });
            }

            return Ok(storeUserPrivilege);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the store user privilege.", detail = ex.Message });
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
            var currentUser = "system"; // TODO: Get from authentication context
            var deleted = await _storeUserService.DeleteStoreUserPrivilegeAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Store user privilege with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the store user privilege.", detail = ex.Message });
        }
    }

    #endregion
}