using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Auth;

/// <summary>
/// Service for bootstrapping the application with initial admin user and permissions.
/// </summary>
public interface IBootstrapService
{
    /// <summary>
    /// Ensures the database has initial admin user and permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if bootstrap was successful</returns>
    Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the default admin user if it doesn't exist.
    /// </summary>
    /// <param name="username">Admin username</param>
    /// <param name="email">Admin email</param>
    /// <param name="password">Admin password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> CreateDefaultAdminAsync(string username, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default roles and permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SeedDefaultRolesAndPermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Bootstrap configuration options.
/// </summary>
public class BootstrapOptions
{
    /// <summary>
    /// Default admin username.
    /// </summary>
    public string DefaultAdminUsername { get; set; } = "admin";

    /// <summary>
    /// Default admin email.
    /// </summary>
    public string DefaultAdminEmail { get; set; } = "admin@eventforge.com";

    /// <summary>
    /// Default admin password.
    /// </summary>
    public string DefaultAdminPassword { get; set; } = "EventForge@2024!";

    /// <summary>
    /// Auto-create admin on startup.
    /// </summary>
    public bool AutoCreateAdmin { get; set; } = true;
}

/// <summary>
/// Implementation of bootstrap service.
/// </summary>
public class BootstrapService : IBootstrapService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<BootstrapService> _logger;
    private readonly BootstrapOptions _options;

    public BootstrapService(
        EventForgeDbContext dbContext,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<BootstrapService> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _logger = logger;
        _options = configuration.GetSection("Authentication:Bootstrap").Get<BootstrapOptions>() ?? new BootstrapOptions();
    }

    public async Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure database is created
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

            // Seed default roles and permissions
            if (!await SeedDefaultRolesAndPermissionsAsync(cancellationToken))
            {
                _logger.LogError("Failed to seed default roles and permissions");
                return false;
            }

            // Create default admin if enabled and doesn't exist
            if (_options.AutoCreateAdmin)
            {
                var adminExists = await _dbContext.Users
                    .AnyAsync(u => u.Username == _options.DefaultAdminUsername, cancellationToken);

                if (!adminExists)
                {
                    if (!await CreateDefaultAdminAsync(
                        _options.DefaultAdminUsername,
                        _options.DefaultAdminEmail,
                        _options.DefaultAdminPassword,
                        cancellationToken))
                    {
                        _logger.LogError("Failed to create default admin user");
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation("Default admin user already exists");
                }
            }

            _logger.LogInformation("Admin bootstrap completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin bootstrap");
            return false;
        }
    }

    public async Task<bool> CreateDefaultAdminAsync(string username, string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate password
            var validation = _passwordService.ValidatePassword(password);
            if (!validation.IsValid)
            {
                _logger.LogError("Default admin password does not meet policy requirements: {Errors}",
                    string.Join(", ", validation.Errors));
                return false;
            }

            // Hash password
            var (hash, salt) = _passwordService.HashPassword(password);

            // Create admin user
            var adminUser = new User
            {
                Username = username,
                Email = email,
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(adminUser);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign admin role
            var adminRole = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

            if (adminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    GrantedBy = "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.UserRoles.Add(userRole);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Default admin user created: {Username} ({Email})", username, email);
            _logger.LogWarning("SECURITY: Default admin password is being used. Please change it immediately after first login!");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default admin user");
            return false;
        }
    }

    public async Task<bool> SeedDefaultRolesAndPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Define default permissions
            var defaultPermissions = new[]
            {
                // User management
                new Permission { Name = "Users.Users.Create", DisplayName = "Create Users", Category = "Users", Resource = "Users", Action = "Create", IsSystemPermission = true },
                new Permission { Name = "Users.Users.Read", DisplayName = "View Users", Category = "Users", Resource = "Users", Action = "Read", IsSystemPermission = true },
                new Permission { Name = "Users.Users.Update", DisplayName = "Update Users", Category = "Users", Resource = "Users", Action = "Update", IsSystemPermission = true },
                new Permission { Name = "Users.Users.Delete", DisplayName = "Delete Users", Category = "Users", Resource = "Users", Action = "Delete", IsSystemPermission = true },

                // Role management
                new Permission { Name = "Users.Roles.Create", DisplayName = "Create Roles", Category = "Users", Resource = "Roles", Action = "Create", IsSystemPermission = true },
                new Permission { Name = "Users.Roles.Read", DisplayName = "View Roles", Category = "Users", Resource = "Roles", Action = "Read", IsSystemPermission = true },
                new Permission { Name = "Users.Roles.Update", DisplayName = "Update Roles", Category = "Users", Resource = "Roles", Action = "Update", IsSystemPermission = true },
                new Permission { Name = "Users.Roles.Delete", DisplayName = "Delete Roles", Category = "Users", Resource = "Roles", Action = "Delete", IsSystemPermission = true },

                // Events
                new Permission { Name = "Events.Events.Create", DisplayName = "Create Events", Category = "Events", Resource = "Events", Action = "Create", IsSystemPermission = true },
                new Permission { Name = "Events.Events.Read", DisplayName = "View Events", Category = "Events", Resource = "Events", Action = "Read", IsSystemPermission = true },
                new Permission { Name = "Events.Events.Update", DisplayName = "Update Events", Category = "Events", Resource = "Events", Action = "Update", IsSystemPermission = true },
                new Permission { Name = "Events.Events.Delete", DisplayName = "Delete Events", Category = "Events", Resource = "Events", Action = "Delete", IsSystemPermission = true },

                // Teams
                new Permission { Name = "Events.Teams.Create", DisplayName = "Create Teams", Category = "Events", Resource = "Teams", Action = "Create", IsSystemPermission = true },
                new Permission { Name = "Events.Teams.Read", DisplayName = "View Teams", Category = "Events", Resource = "Teams", Action = "Read", IsSystemPermission = true },
                new Permission { Name = "Events.Teams.Update", DisplayName = "Update Teams", Category = "Events", Resource = "Teams", Action = "Update", IsSystemPermission = true },
                new Permission { Name = "Events.Teams.Delete", DisplayName = "Delete Teams", Category = "Events", Resource = "Teams", Action = "Delete", IsSystemPermission = true },

                // Reports
                new Permission { Name = "Reports.Reports.Read", DisplayName = "View Reports", Category = "Reports", Resource = "Reports", Action = "Read", IsSystemPermission = true },
                new Permission { Name = "Reports.Audit.Read", DisplayName = "View Audit Logs", Category = "Reports", Resource = "Audit", Action = "Read", IsSystemPermission = true },

                // System
                new Permission { Name = "System.Settings.Update", DisplayName = "Update System Settings", Category = "System", Resource = "Settings", Action = "Update", IsSystemPermission = true },
                new Permission { Name = "System.Logs.Read", DisplayName = "View System Logs", Category = "System", Resource = "Logs", Action = "Read", IsSystemPermission = true }
            };

            // Add permissions if they don't exist
            foreach (var permission in defaultPermissions)
            {
                var existingPermission = await _dbContext.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name, cancellationToken);

                if (existingPermission == null)
                {
                    permission.CreatedBy = "system";
                    permission.CreatedAt = DateTime.UtcNow;
                    _dbContext.Permissions.Add(permission);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Define default roles
            var defaultRoles = new[]
            {
                new Role { Name = "Admin", DisplayName = "System Administrator", Description = "Full system access", IsSystemRole = true },
                new Role { Name = "Manager", DisplayName = "Manager", Description = "Management level access", IsSystemRole = true },
                new Role { Name = "User", DisplayName = "Standard User", Description = "Basic user access", IsSystemRole = true },
                new Role { Name = "Viewer", DisplayName = "Viewer", Description = "Read-only access", IsSystemRole = true }
            };

            // Add roles if they don't exist
            foreach (var role in defaultRoles)
            {
                var existingRole = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.Name == role.Name, cancellationToken);

                if (existingRole == null)
                {
                    role.CreatedBy = "system";
                    role.CreatedAt = DateTime.UtcNow;
                    _dbContext.Roles.Add(role);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign permissions to Admin role
            var adminRole = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

            if (adminRole != null && !adminRole.RolePermissions.Any())
            {
                var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

                foreach (var permission in allPermissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id,
                        GrantedBy = "system",
                        GrantedAt = DateTime.UtcNow,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.RolePermissions.Add(rolePermission);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Default roles and permissions seeded successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default roles and permissions");
            return false;
        }
    }
}