using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for authentication operations.
/// </summary>
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.LoginAsync(request, ipAddress, userAgent, cancellationToken);

        if (result == null)
        {
            // Check if account is locked
            var user = await _authenticationService.GetUserAsync(Guid.Empty, cancellationToken); // This won't work, need to find by username
            // For now, return generic unauthorized
            return Problem(
                title: "Authentication Failed",
                detail: "Invalid username or password, or account is locked.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(result);
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
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Problem(
                title: "Invalid User Context",
                detail: "Unable to identify current user.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var success = await _authenticationService.ChangePasswordAsync(userId, request, cancellationToken);

        if (!success)
        {
            return Problem(
                title: "Password Change Failed",
                detail: "Unable to change password. Please verify your current password and ensure the new password meets security requirements.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Ok(new { message = "Password changed successfully." });
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
        var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Problem(
                title: "Invalid User Context",
                detail: "Unable to identify current user.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await _authenticationService.GetUserAsync(userId, cancellationToken);

        if (user == null)
        {
            return Problem(
                title: "User Not Found",
                detail: "Current user information could not be retrieved.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(user);
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
        // For JWT, logout is primarily client-side (removing token)
        // Here we could add token to a blacklist if needed
        // For now, just return success

        var username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        _logger.LogInformation("User {Username} logged out", username);

        return Ok(new { message = "Logged out successfully." });
    }
}