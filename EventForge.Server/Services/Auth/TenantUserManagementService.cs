using EventForge.Server.Mappers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth;

/// <summary>
/// Service for tenant-scoped user management operations.
/// Filters all operations to the current tenant context and validates access permissions.
/// </summary>
public class TenantUserManagementService : ITenantUserManagementService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordService _passwordService;
    private readonly IAuditLogService _auditLogService;
    private readonly IHubContext<AuditLogHub> _hubContext;
    private readonly ILogger<TenantUserManagementService> _logger;

    public TenantUserManagementService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IPasswordService passwordService,
        IAuditLogService auditLogService,
        IHubContext<AuditLogHub> hubContext,
        ILogger<TenantUserManagementService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _passwordService = passwordService;
        _auditLogService = auditLogService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Validates that the current user can access the specified tenant.
    /// </summary>
    private async Task ValidateTenantAccessAsync(Guid tenantId)
    {
        if (!await _tenantContext.CanAccessTenantAsync(tenantId))
        {
            throw new UnauthorizedAccessException("Access to this tenant is not allowed");
        }
    }

    /// <summary>
    /// Gets the current tenant ID or throws if not set.
    /// </summary>
    private Guid GetCurrentTenantId()
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is not set");
        }
        return _tenantContext.CurrentTenantId.Value;
    }

    /// <summary>
    /// Gets the current user ID or throws if not set.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        if (!_tenantContext.CurrentUserId.HasValue)
        {
            throw new InvalidOperationException("User context is not set");
        }
        return _tenantContext.CurrentUserId.Value;
    }

    public async Task<IEnumerable<UserManagementDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Retrieving all users for tenant {TenantId}", tenantId);

        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        var result = users.Select(u => UserMapper.ToManagementDto(
            u,
            u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            u.Tenant?.Name
        )).ToList();

        _logger.LogInformation("Retrieved {Count} users for tenant {TenantId}", result.Count, tenantId);
        return result;
    }

    public async Task<UserManagementDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found in tenant {TenantId}", userId, tenantId);
            return null;
        }

        return UserMapper.ToManagementDto(
            user,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.Tenant?.Name
        );
    }

    public async Task<IEnumerable<UserManagementDto>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Searching users in tenant {TenantId} with query: {Query}", tenantId, query);

        var lowerQuery = query.ToLower();
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.TenantId == tenantId &&
                       (u.Username.ToLower().Contains(lowerQuery) ||
                        u.Email.ToLower().Contains(lowerQuery) ||
                        u.FirstName.ToLower().Contains(lowerQuery) ||
                        u.LastName.ToLower().Contains(lowerQuery)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        return users.Select(u => UserMapper.ToManagementDto(
            u,
            u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            u.Tenant?.Name
        )).ToList();
    }

    public async Task<CreatedUserDto> CreateUserAsync(CreateUserManagementDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        // Override TenantId to current tenant (tenant admins can only create users in their own tenant)
        dto.TenantId = tenantId;

        _logger.LogInformation("Creating new user {Username} in tenant {TenantId}", dto.Username, tenantId);

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' already exists");
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{dto.Email}' already exists");
        }

        // Generate initial password
        var initialPassword = GenerateRandomPassword();
        var (hash, salt) = _passwordService.HashPassword(initialPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            TenantId = tenantId,
            PasswordHash = hash,
            PasswordSalt = salt,
            MustChangePassword = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Assign roles
        if (dto.Roles.Any())
        {
            var roles = await _context.Roles
                .Where(r => dto.Roles.Contains(r.Name))
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "User",
            user.Id,
            "User",
            "Insert",
            null,
            $"Created user {user.Username}",
            GetCurrentUserId().ToString(),
            user.Username,
            cancellationToken
        );

        _logger.LogInformation("User {Username} created successfully with ID {UserId}. Initial password must be securely transmitted to the user.",
            dto.Username, user.Id);

        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
        var userManagementDto = UserMapper.ToManagementDto(user, dto.Roles, tenant?.Name);

        // Return the created user with the initial password
        return new CreatedUserDto
        {
            Id = userManagementDto.Id,
            Username = userManagementDto.Username,
            Email = userManagementDto.Email,
            FirstName = userManagementDto.FirstName,
            LastName = userManagementDto.LastName,
            FullName = userManagementDto.FullName,
            TenantId = userManagementDto.TenantId,
            TenantName = userManagementDto.TenantName,
            Roles = userManagementDto.Roles,
            IsActive = userManagementDto.IsActive,
            MustChangePassword = userManagementDto.MustChangePassword,
            CreatedAt = userManagementDto.CreatedAt,
            LastLoginAt = userManagementDto.LastLoginAt,
            InitialPassword = initialPassword
        };
    }

    public async Task<UserManagementDto> UpdateUserAsync(Guid userId, UpdateUserManagementDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Updating user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        // Track changes for audit
        var changes = new List<string>();

        if (user.Email != dto.Email)
        {
            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "Email", "Update",
                user.Email, dto.Email,
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            changes.Add($"Email: {user.Email} -> {dto.Email}");
            user.Email = dto.Email;
        }

        if (user.FirstName != dto.FirstName)
        {
            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "FirstName", "Update",
                user.FirstName, dto.FirstName,
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            changes.Add($"FirstName: {user.FirstName} -> {dto.FirstName}");
            user.FirstName = dto.FirstName;
        }

        if (user.LastName != dto.LastName)
        {
            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "LastName", "Update",
                user.LastName, dto.LastName,
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            changes.Add($"LastName: {user.LastName} -> {dto.LastName}");
            user.LastName = dto.LastName;
        }

        if (user.IsActive != dto.IsActive)
        {
            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "IsActive", "Update",
                user.IsActive.ToString(), dto.IsActive.ToString(),
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            changes.Add($"IsActive: {user.IsActive} -> {dto.IsActive}");
            user.IsActive = dto.IsActive;
        }

        user.ModifiedAt = DateTime.UtcNow;

        // Update roles if changed
        var currentRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var rolesChanged = !currentRoles.OrderBy(r => r).SequenceEqual(dto.Roles.OrderBy(r => r));

        if (rolesChanged)
        {
            // Remove old roles
            _context.UserRoles.RemoveRange(user.UserRoles);

            // Add new roles
            var roles = await _context.Roles
                .Where(r => dto.Roles.Contains(r.Name))
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
            }

            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "Roles", "Update",
                string.Join(", ", currentRoles),
                string.Join(", ", dto.Roles),
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            changes.Add($"Roles: [{string.Join(", ", currentRoles)}] -> [{string.Join(", ", dto.Roles)}]");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated successfully. Changes: {Changes}",
            userId, string.Join("; ", changes));

        return UserMapper.ToManagementDto(user, dto.Roles, user.Tenant?.Name);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Deleting user {UserId} from tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        // Prevent self-deletion
        if (userId == GetCurrentUserId())
        {
            throw new InvalidOperationException("Cannot delete your own user account");
        }

        // Remove user roles first
        _context.UserRoles.RemoveRange(user.UserRoles);

        // Soft delete by deactivating
        user.IsActive = false;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "User",
            userId,
            "IsActive",
            "Delete",
            "true",
            "false",
            GetCurrentUserId().ToString(),
            user.Username,
            cancellationToken
        );

        _logger.LogInformation("User {UserId} deleted (deactivated) successfully", userId);
    }

    public async Task<UserManagementDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Updating status for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        if (user.IsActive != dto.IsActive)
        {
            await _auditLogService.LogEntityChangeAsync(
                "User", userId, "IsActive", "Update",
                user.IsActive.ToString(), dto.IsActive.ToString(),
                GetCurrentUserId().ToString(), user.Username,
                cancellationToken
            );
            user.IsActive = dto.IsActive;
            user.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        return UserMapper.ToManagementDto(
            user,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.Tenant?.Name
        );
    }

    public async Task<UserManagementDto> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Updating roles for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        var currentRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Remove old roles
        _context.UserRoles.RemoveRange(user.UserRoles);

        // Add new roles
        var roleEntities = await _context.Roles
            .Where(r => roles.Contains(r.Name))
            .ToListAsync(cancellationToken);

        foreach (var role in roleEntities)
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
        }

        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "User", userId, "Roles", "Update",
            string.Join(", ", currentRoles),
            string.Join(", ", roles),
            GetCurrentUserId().ToString(), user.Username,
            cancellationToken
        );

        _logger.LogInformation("User {UserId} roles updated successfully", userId);

        return UserMapper.ToManagementDto(user, roles, user.Tenant?.Name);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword = true, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Resetting password for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        // Validate password
        var validationResult = _passwordService.ValidatePassword(newPassword);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Password validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        var (hash, salt) = _passwordService.HashPassword(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = mustChangePassword;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "User", userId, "Password", "Update",
            null, "***RESET***",
            GetCurrentUserId().ToString(), user.Username,
            cancellationToken
        );

        _logger.LogInformation("Password reset successfully for user {UserId}", userId);
    }

    public async Task ForcePasswordChangeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Forcing password change for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        user.MustChangePassword = true;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "User", userId, "MustChangePassword", "Update",
            "false", "true",
            GetCurrentUserId().ToString(), user.Username,
            cancellationToken
        );

        _logger.LogInformation("Password change forced for user {UserId}", userId);
    }

    public async Task<object> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Retrieving user statistics for tenant {TenantId}", tenantId);

        var totalUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken);
        var activeUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, cancellationToken);
        var inactiveUsers = totalUsers - activeUsers;
        var usersRequiringPasswordChange = await _context.Users.CountAsync(
            u => u.TenantId == tenantId && u.MustChangePassword, cancellationToken);

        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        var recentLogins = await _context.Users.CountAsync(
            u => u.TenantId == tenantId && u.LastLoginAt.HasValue && u.LastLoginAt.Value >= oneMonthAgo,
            cancellationToken);

        return new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            UsersRequiringPasswordChange = usersRequiringPasswordChange,
            RecentLogins = recentLogins,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<UserManagementDto> PerformQuickActionAsync(Guid userId, string action, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await ValidateTenantAccessAsync(tenantId);

        _logger.LogInformation("Performing quick action {Action} for user {UserId} in tenant {TenantId}",
            action, userId, tenantId);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Tenant)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found in current tenant");
        }

        switch (action.ToLower())
        {
            case "lock":
                user.LockedUntil = DateTime.UtcNow.AddDays(30);
                await _auditLogService.LogEntityChangeAsync(
                    "User", userId, "LockedUntil", "Update",
                    null, user.LockedUntil.ToString(),
                    GetCurrentUserId().ToString(), user.Username,
                    cancellationToken
                );
                break;

            case "unlock":
                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;
                await _auditLogService.LogEntityChangeAsync(
                    "User", userId, "LockedUntil", "Update",
                    "locked", "null",
                    GetCurrentUserId().ToString(), user.Username,
                    cancellationToken
                );
                break;

            case "activate":
                user.IsActive = true;
                await _auditLogService.LogEntityChangeAsync(
                    "User", userId, "IsActive", "Update",
                    "false", "true",
                    GetCurrentUserId().ToString(), user.Username,
                    cancellationToken
                );
                break;

            case "deactivate":
                user.IsActive = false;
                await _auditLogService.LogEntityChangeAsync(
                    "User", userId, "IsActive", "Update",
                    "true", "false",
                    GetCurrentUserId().ToString(), user.Username,
                    cancellationToken
                );
                break;

            default:
                throw new InvalidOperationException($"Unknown action: {action}");
        }

        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quick action {Action} performed successfully for user {UserId}", action, userId);

        return UserMapper.ToManagementDto(
            user,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.Tenant?.Name
        );
    }

    /// <summary>
    /// Generates a random password that meets security requirements using cryptographically secure random number generation.
    /// </summary>
    private string GenerateRandomPassword()
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var password = new char[12];

        // Ensure at least one of each required character type using cryptographically secure random
        password[0] = lowercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[1] = uppercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[System.Security.Cryptography.RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest randomly
        var allChars = lowercase + uppercase + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle the password using Fisher-Yates algorithm with cryptographically secure random
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
