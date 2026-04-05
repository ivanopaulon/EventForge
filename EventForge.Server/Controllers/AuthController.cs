using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for authentication operations.
/// </summary>
[Route("api/v1/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger) : BaseApiController
{

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token</returns>
    /// <response code="200">Returns authentication result with JWT token</response>
    /// <response code="400">If login credentials are invalid</response>
    /// <response code="401">If authentication fails</response>
    /// <response code="423">If account is locked</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await authenticationService.LoginAsync(request, ipAddress, userAgent, cancellationToken);

            if (result is null)
                return CreateUnauthorizedProblem("Invalid username or password, or account is locked.");

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Login");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">If password was changed successfully</response>
    /// <response code="400">If request is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If current password is incorrect</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return CreateUnauthorizedProblem("Unable to identify current user.");

            var success = await authenticationService.ChangePasswordAsync(userId, request, cancellationToken);

            if (!success)
                return CreateForbiddenProblem("Unable to change password. Please verify your current password and ensure the new password meets security requirements.");

            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ChangePassword");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }

    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information</returns>
    /// <response code="200">Returns current user information</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If user is not found</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return CreateUnauthorizedProblem("Unable to identify current user.");

            var user = await authenticationService.GetUserAsync(userId, cancellationToken);

            if (user is null)
                return CreateNotFoundProblem("Current user information could not be retrieved.");

            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetCurrentUser");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }

    /// <summary>
    /// Validates the current JWT token.
    /// </summary>
    /// <returns>Token validation result</returns>
    /// <response code="200">If token is valid</response>
    /// <response code="401">If token is invalid or expired</response>
    [HttpPost("validate-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        try
        {
            // If we reach here, the token is valid (passed authorization)
            var username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            var userId = HttpContext.User.FindFirst("user_id")?.Value;
            var roles = HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var permissions = HttpContext.User.FindAll("permission").Select(c => c.Value).ToArray();

            return Ok(new
            {
                valid = true,
                username,
                userId,
                roles,
                permissions,
                message = "Token is valid."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ValidateToken");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }

    /// <summary>
    /// Refreshes the JWT token for the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT token</returns>
    /// <response code="200">Returns new JWT token</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If token refresh is not allowed</response>
    [HttpPost("refresh-token")]
    [Authorize]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken(CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return CreateUnauthorizedProblem("Unable to identify current user.");

            var result = await authenticationService.RefreshTokenAsync(userId, cancellationToken);

            if (result is null)
                return CreateForbiddenProblem("Unable to refresh token. User may be inactive or account locked.");

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RefreshToken");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <returns>Logout result</returns>
    /// <response code="200">If logout was successful</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        try
        {
            var username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            logger.LogInformation("User {Username} logged out", username);

            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Logout");
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }
}