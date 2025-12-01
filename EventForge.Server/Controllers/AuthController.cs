using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventForge.Server.Controllers;

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
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Authentication Failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid username or password, or account is locked.",
                Instance = HttpContext.Request.Path
            };

            // Add correlation ID if available
            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return Unauthorized(problemDetails);
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
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Invalid User Context",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Unable to identify current user.",
                Instance = HttpContext.Request.Path
            };

            // Add correlation ID if available
            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return Unauthorized(problemDetails);
        }

        var success = await _authenticationService.ChangePasswordAsync(userId, request, cancellationToken);

        if (!success)
        {
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Password Change Failed",
                Status = StatusCodes.Status403Forbidden,
                Detail = "Unable to change password. Please verify your current password and ensure the new password meets security requirements.",
                Instance = HttpContext.Request.Path
            };

            // Add correlation ID if available
            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId2))
            {
                problemDetails.Extensions["correlationId"] = correlationId2;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return StatusCode(StatusCodes.Status403Forbidden, problemDetails);
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
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Invalid User Context",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Unable to identify current user.",
                Instance = HttpContext.Request.Path
            };

            // Add correlation ID if available
            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return Unauthorized(problemDetails);
        }

        var user = await _authenticationService.GetUserAsync(userId, cancellationToken);

        if (user == null)
        {
            return CreateNotFoundProblem("Current user information could not be retrieved.");
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
        var userIdClaim = HttpContext.User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Invalid User Context",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Unable to identify current user.",
                Instance = HttpContext.Request.Path
            };

            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return Unauthorized(problemDetails);
        }

        var result = await _authenticationService.RefreshTokenAsync(userId, cancellationToken);

        if (result == null)
        {
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Token Refresh Failed",
                Status = StatusCodes.Status403Forbidden,
                Detail = "Unable to refresh token. User may be inactive or account locked.",
                Instance = HttpContext.Request.Path
            };

            if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId2))
            {
                problemDetails.Extensions["correlationId"] = correlationId2;
            }

            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return StatusCode(StatusCodes.Status403Forbidden, problemDetails);
        }

        return Ok(result);
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