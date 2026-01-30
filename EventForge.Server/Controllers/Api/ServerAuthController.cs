using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Auth;

namespace EventForge.Server.Controllers.Api;

/// <summary>
/// REST API controller for server authentication operations (multi-tenant login for admins).
/// </summary>
[Route("api/v1/auth")]
[ApiController]
[Produces("application/json")]
public class ServerAuthController : ControllerBase
{
    private readonly EventForgeDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<ServerAuthController> _logger;

    public ServerAuthController(
        EventForgeDbContext context,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ILogger<ServerAuthController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets list of active tenants for login selection.
    /// </summary>
    /// <returns>List of active tenants</returns>
    /// <response code="200">Returns list of active tenants</response>
    [HttpGet("tenants")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantDto>>> GetTenants()
    {
        var tenants = await _context.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name
            })
            .ToListAsync();

        return Ok(tenants);
    }

    /// <summary>
    /// Authenticates a user for server-side access (dashboard, settings).
    /// Only users with Admin or SuperAdmin roles can authenticate.
    /// </summary>
    /// <param name="request">Login credentials with tenant selection</param>
    /// <returns>Authentication result with JWT token</returns>
    /// <response code="200">Returns authentication result with JWT token</response>
    /// <response code="400">If login credentials are invalid</response>
    /// <response code="401">If authentication fails or user lacks admin privileges</response>
    [HttpPost("server-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServerLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServerLoginResponseDto>> ServerLogin([FromBody] ServerLoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find user in database with tenant and include navigation
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username &&
                u.TenantId == request.TenantId &&
                u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Login attempt failed for username {Username} in tenant {TenantId}: User not found or inactive",
                request.Username, request.TenantId);

            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Authentication Failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid credentials",
                Instance = HttpContext.Request.Path
            });
        }

        // Check if account is locked
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account: {Username} in tenant {TenantId}",
                request.Username, request.TenantId);

            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Account Locked",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Account is temporarily locked. Please try again later.",
                Instance = HttpContext.Request.Path
            });
        }

        // Verify password using Argon2
        var isPasswordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!isPasswordValid)
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;
            
            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                _logger.LogWarning("Account locked due to failed login attempts: {Username} in tenant {TenantId}",
                    user.Username, request.TenantId);
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Login attempt failed for username {Username} in tenant {TenantId}: Invalid password",
                request.Username, request.TenantId);

            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Authentication Failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid credentials",
                Instance = HttpContext.Request.Path
            });
        }

        // Get user roles
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        // Check if user has Admin or SuperAdmin role
        if (!roles.Contains("SuperAdmin") && !roles.Contains("Admin"))
        {
            _logger.LogWarning("Login attempt by non-admin user: {Username} in tenant {TenantId}",
                request.Username, request.TenantId);

            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Access Denied",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Server access requires Admin or SuperAdmin role",
                Instance = HttpContext.Request.Path
            });
        }

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _context.SaveChangesAsync();

        // Get user permissions
        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(_context.Permissions, rp => rp, p => p.Id, (rp, p) => p.Name)
            .Distinct()
            .ToListAsync();

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user, user.Tenant, roles, permissions);

        _logger.LogInformation("Successful server login for user {Username} in tenant {TenantName} with roles: {Roles}",
            user.Username, user.Tenant.Name, string.Join(", ", roles));

        return Ok(new ServerLoginResponseDto
        {
            Token = token,
            Username = user.Username,
            TenantName = user.Tenant.Name,
            Roles = roles
        });
    }

    /// <summary>
    /// Verifies the current JWT token and returns user information.
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">Returns current user information</response>
    /// <response code="401">If token is invalid or expired</response>
    [HttpGet("verify")]
    [Authorize]
    [ProducesResponseType(typeof(VerifyTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<VerifyTokenResponseDto> VerifyToken()
    {
        var username = User.Identity?.Name;
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new VerifyTokenResponseDto
        {
            Username = username ?? "Unknown",
            Roles = roles
        });
    }
}

/// <summary>
/// DTO for server login request.
/// </summary>
public class ServerLoginRequestDto
{
    [Required(ErrorMessage = "Tenant is required")]
    public Guid TenantId { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO for server login response.
/// </summary>
public class ServerLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// DTO for token verification response.
/// </summary>
public class VerifyTokenResponseDto
{
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
