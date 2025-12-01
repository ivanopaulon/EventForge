using EventForge.DTOs.SuperAdmin;
using EventForge.DTOs.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant-scoped user management operations.
/// Allows tenant administrators to manage users within their own tenant.
/// </summary>
[Route("api/v1/tenant/user-management")]
[ApiController]
public class TenantUserManagementController : BaseApiController
{
    private readonly ITenantUserManagementService _tenantUserService;
    private readonly ILogger<TenantUserManagementController> _logger;

    public TenantUserManagementController(
        ITenantUserManagementService tenantUserService,
        ILogger<TenantUserManagementController> logger)
    {
        _tenantUserService = tenantUserService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users in the current tenant.
    /// </summary>
    /// <returns>List of users in the current tenant</returns>
    /// <response code="200">Returns the list of users</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a tenant admin</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(IEnumerable<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _tenantUserService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to tenant users");
            return CreateForbiddenProblem("Access to tenant users is not allowed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant users");
            return CreateInternalServerErrorProblem("Error retrieving users", ex);
        }
    }

    /// <summary>
    /// Gets a specific user by ID within the current tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    /// <response code="200">Returns the user information</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a tenant admin</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{userId}")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _tenantUserService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return CreateNotFoundProblem($"User {userId} not found in current tenant");
            }
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to user {UserId}", userId);
            return CreateForbiddenProblem("Access to this user is not allowed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error retrieving user", ex);
        }
    }

    /// <summary>
    /// Searches users in the current tenant.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching users</returns>
    /// <response code="200">Returns the matching users</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a tenant admin</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("search")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(IEnumerable<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> SearchUsers([FromQuery] string query, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            var users = await _tenantUserService.SearchUsersAsync(query, cancellationToken);
            return Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized search access");
            return CreateForbiddenProblem("Search access is not allowed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query: {Query}", query);
            return CreateInternalServerErrorProblem("Error searching users", ex);
        }
    }

    /// <summary>
    /// Creates a new user in the current tenant.
    /// </summary>
    /// <param name="dto">User creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> CreateUser([FromBody] CreateTenantUserDto dto, CancellationToken cancellationToken)
    {
        try
        {
            // Map CreateTenantUserDto to CreateUserManagementDto
            var createDto = new CreateUserManagementDto
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                TenantId = Guid.Empty, // Will be set by the service
                Roles = dto.Roles
            };

            var user = await _tenantUserService.CreateUserAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized user creation");
            return CreateForbiddenProblem("User creation is not allowed");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return CreateInternalServerErrorProblem("Error creating user", ex);
        }
    }

    /// <summary>
    /// Updates an existing user in the current tenant.
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="dto">Updated user data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">User updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{userId}")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(Guid userId, [FromBody] UpdateUserManagementDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _tenantUserService.UpdateUserAsync(userId, dto, cancellationToken);
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized user update for {UserId}", userId);
            return CreateForbiddenProblem("User update is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error updating user", ex);
        }
    }

    /// <summary>
    /// Deletes a user from the current tenant.
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">User deleted successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{userId}")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _tenantUserService.DeleteUserAsync(userId, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized user deletion for {UserId}", userId);
            return CreateForbiddenProblem("User deletion is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user deletion request for {UserId}", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error deleting user", ex);
        }
    }

    /// <summary>
    /// Updates user status (active/inactive).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Status update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("{userId}/status")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _tenantUserService.UpdateUserStatusAsync(userId, dto, cancellationToken);
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized status update for {UserId}", userId);
            return CreateForbiddenProblem("Status update is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error updating status", ex);
        }
    }

    /// <summary>
    /// Updates user roles within the current tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">New list of role names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Roles updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("{userId}/roles")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> UpdateUserRoles(Guid userId, [FromBody] List<string> roles, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _tenantUserService.UpdateUserRolesAsync(userId, roles, cancellationToken);
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized roles update for {UserId}", userId);
            return CreateForbiddenProblem("Roles update is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error updating roles", ex);
        }
    }

    /// <summary>
    /// Resets a user's password.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Password reset request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Password reset successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{userId}/reset-password")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _tenantUserService.ResetPasswordAsync(userId, request.NewPassword, request.MustChangePassword, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized password reset for {UserId}", userId);
            return CreateForbiddenProblem("Password reset is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid password reset request for {UserId}", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error resetting password", ex);
        }
    }

    /// <summary>
    /// Forces a user to change password on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Password change forced successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{userId}/force-password-change")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ForcePasswordChange(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _tenantUserService.ForcePasswordChangeAsync(userId, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized force password change for {UserId}", userId);
            return CreateForbiddenProblem("Force password change is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing password change for user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error forcing password change", ex);
        }
    }

    /// <summary>
    /// Gets user statistics for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User statistics</returns>
    /// <response code="200">Returns user statistics</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a tenant admin</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("statistics")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _tenantUserService.GetUserStatisticsAsync(cancellationToken);
            return Ok(statistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to statistics");
            return CreateForbiddenProblem("Access to statistics is not allowed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics");
            return CreateInternalServerErrorProblem("Error retrieving statistics", ex);
        }
    }

    /// <summary>
    /// Performs quick actions on a user (lock, unlock, activate, deactivate).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Quick action request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Action performed successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have full access</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("{userId}/quick-actions")]
    [Authorize(Policy = "RequireTenantFullAccess")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> PerformQuickAction(Guid userId, [FromBody] QuickActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _tenantUserService.PerformQuickActionAsync(userId, request.Action, cancellationToken);
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized quick action for {UserId}", userId);
            return CreateForbiddenProblem("Quick action is not allowed");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found", userId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid quick action for {UserId}", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing quick action for user {UserId}", userId);
            return CreateInternalServerErrorProblem("Error performing quick action", ex);
        }
    }
}

/// <summary>
/// Request model for password reset.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// New password for the user.
    /// </summary>
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Whether user must change password on next login.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;
}

/// <summary>
/// Request model for quick actions.
/// </summary>
public class QuickActionRequest
{
    /// <summary>
    /// Action to perform (lock, unlock, activate, deactivate).
    /// </summary>
    [Required]
    public string Action { get; set; } = string.Empty;
}
