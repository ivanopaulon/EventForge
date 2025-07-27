using Microsoft.EntityFrameworkCore;
using EventForge.Server.Mappers;

namespace EventForge.Server.Services.Auth;

/// <summary>
/// Service for user authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response if successful</returns>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Change password request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information by ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a user account is locked.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if account is locked</returns>
    Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Account lockout configuration.
/// </summary>
public class AccountLockoutOptions
{
    /// <summary>
    /// Maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Reset failed attempts after successful login.
    /// </summary>
    public bool ResetFailedAttemptsOnSuccess { get; set; } = true;
}

/// <summary>
/// Implementation of authentication service.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AccountLockoutOptions _lockoutOptions;

    public AuthenticationService(
        EventForgeDbContext dbContext,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _lockoutOptions = configuration.GetSection("Authentication:AccountLockout").Get<AccountLockoutOptions>() ?? new AccountLockoutOptions();
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var loginAudit = new LoginAudit
        {
            Username = request.Username,
            EventType = "Login",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            EventTime = DateTime.UtcNow
        };

        try
        {
            // Find user by username
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

            if (user == null)
            {
                loginAudit.Success = false;
                loginAudit.FailureReason = "Invalid username";
                await _dbContext.LoginAudits.AddAsync(loginAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Login attempt failed: Invalid username {Username} from {IpAddress}", request.Username, ipAddress);
                return null;
            }

            loginAudit.UserId = user.Id;

            // Check if account is locked
            if (user.IsLockedOut)
            {
                loginAudit.Success = false;
                loginAudit.FailureReason = "Account locked";
                await _dbContext.LoginAudits.AddAsync(loginAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Login attempt failed: Account locked for user {Username}", request.Username);
                return null;
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                // Increment failed attempts
                user.FailedLoginAttempts++;
                user.LastFailedLoginAt = DateTime.UtcNow;

                // Lock account if max attempts reached
                if (user.FailedLoginAttempts >= _lockoutOptions.MaxFailedAttempts)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutOptions.LockoutDurationMinutes);
                    _logger.LogWarning("Account locked for user {Username} after {FailedAttempts} failed attempts",
                        request.Username, user.FailedLoginAttempts);
                }

                loginAudit.Success = false;
                loginAudit.FailureReason = "Invalid password";
                await _dbContext.LoginAudits.AddAsync(loginAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Login attempt failed: Invalid password for user {Username}", request.Username);
                return null;
            }

            // Successful login
            user.LastLoginAt = DateTime.UtcNow;
            if (_lockoutOptions.ResetFailedAttemptsOnSuccess)
            {
                user.FailedLoginAttempts = 0;
            }

            // Get roles and permissions
            var roles = user.UserRoles
                .Where(ur => ur.IsActive)
                .Select(ur => ur.Role.Name)
                .ToList();

            var permissions = user.UserRoles
                .Where(ur => ur.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => $"{rp.Permission.Category}.{rp.Permission.Resource}.{rp.Permission.Action}")
                .Distinct()
                .ToList();

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user, roles, permissions);

            loginAudit.Success = true;
            loginAudit.SessionId = Guid.NewGuid().ToString();
            await _dbContext.LoginAudits.AddAsync(loginAudit, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var userDto = UserMapper.ToDto(user, roles, permissions);

            _logger.LogInformation("User {Username} logged in successfully from {IpAddress}", request.Username, ipAddress);

            return new LoginResponseDto
            {
                AccessToken = token,
                ExpiresIn = _jwtTokenService.TokenExpirationSeconds,
                User = userDto,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);

            loginAudit.Success = false;
            loginAudit.FailureReason = "Internal error";
            try
            {
                await _dbContext.LoginAudits.AddAsync(loginAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save login audit");
            }

            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password change attempt for non-existent user {UserId}", userId);
                return false;
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {Username}", user.Username);
                return false;
            }

            // Validate new password
            var validation = _passwordService.ValidatePassword(request.NewPassword);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Password change failed: New password validation failed for user {Username}: {Errors}",
                    user.Username, string.Join(", ", validation.Errors));
                return false;
            }

            // Hash new password
            var (hash, salt) = _passwordService.HashPassword(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {Username}", user.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

            if (user == null)
                return null;

            var roles = user.UserRoles
                .Where(ur => ur.IsActive)
                .Select(ur => ur.Role.Name)
                .ToList();

            var permissions = user.UserRoles
                .Where(ur => ur.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => $"{rp.Permission.Category}.{rp.Permission.Resource}.{rp.Permission.Action}")
                .Distinct()
                .ToList();

            var userDto = UserMapper.ToDto(user, roles, permissions);

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            return user?.IsLockedOut == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account is locked for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return false;

            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account unlocked for user {Username}", user.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user {UserId}", userId);
            return false;
        }
    }
}