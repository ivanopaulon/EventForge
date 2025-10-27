using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of user seeding service.
/// </summary>
public class UserSeeder : IUserSeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UserSeeder> _logger;
    private readonly BootstrapOptions _options;

    public UserSeeder(
        EventForgeDbContext dbContext,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<UserSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _logger = logger;

        // Get password with environment variable -> config -> fallback precedence
        var envPassword = Environment.GetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD");
        var configPassword = configuration["Bootstrap:SuperAdminPassword"];
        var fallbackPassword = "SuperAdmin#2025!";

        _options = configuration.GetSection("Bootstrap").Get<BootstrapOptions>() ?? new BootstrapOptions();
        _options.DefaultAdminPassword = envPassword ?? configPassword ?? fallbackPassword;
    }

    public async Task<User?> CreateSuperAdminUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // If user already exists, return it
            var existing = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == _options.DefaultAdminUsername || u.Email == _options.DefaultAdminEmail, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("SuperAdmin user already exists: {Username}", existing.Username);
                return existing;
            }

            // Validate password
            var validation = _passwordService.ValidatePassword(_options.DefaultAdminPassword);
            if (!validation.IsValid)
            {
                _logger.LogError("SuperAdmin password does not meet policy requirements: {Errors}",
                    string.Join(", ", validation.Errors));
                return null;
            }

            // Hash password
            var (hash, salt) = _passwordService.HashPassword(_options.DefaultAdminPassword);

            // Create SuperAdmin user
            var superAdminUser = new User
            {
                Username = _options.DefaultAdminUsername,
                Email = _options.DefaultAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                TenantId = tenantId,
                IsActive = true,
                MustChangePassword = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _ = _dbContext.Users.Add(superAdminUser);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign SuperAdmin role if exists
            var superAdminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);
            if (superAdminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = superAdminUser.Id,
                    RoleId = superAdminRole.Id,
                    GrantedBy = "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                };

                _ = _dbContext.UserRoles.Add(userRole);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("SuperAdmin role assigned to user: {Username}", superAdminUser.Username);
            }
            else
            {
                _logger.LogWarning("SuperAdmin role not found. User created without role assignment.");
            }

            _logger.LogInformation("SuperAdmin user created: {Username} ({Email})", superAdminUser.Username, superAdminUser.Email);
            return superAdminUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SuperAdmin user");
            return null;
        }
    }

    public async Task<User?> CreateDefaultManagerUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string managerUsername = "manager";
        const string managerEmail = "manager@localhost";
        const string managerPassword = "Manager@2025!";

        try
        {
            var existing = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == managerUsername || u.Email == managerEmail, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Manager user already exists: {Username}", existing.Username);
                return existing;
            }

            var validation = _passwordService.ValidatePassword(managerPassword);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Default manager password does not meet policy requirements: {Errors}",
                    string.Join(", ", validation.Errors));
                // Still proceed but log warning
            }

            var (hash, salt) = _passwordService.HashPassword(managerPassword);

            var managerUser = new User
            {
                Username = managerUsername,
                Email = managerEmail,
                FirstName = "Default",
                LastName = "Manager",
                PasswordHash = hash,
                PasswordSalt = salt,
                TenantId = tenantId,
                IsActive = true,
                MustChangePassword = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _ = _dbContext.Users.Add(managerUser);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign Manager role
            var managerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Manager", cancellationToken);
            if (managerRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = managerUser.Id,
                    RoleId = managerRole.Id,
                    GrantedBy = "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                };

                _ = _dbContext.UserRoles.Add(userRole);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Manager role assigned to user: {Username}", managerUser.Username);

                // Assign management permissions to Manager role
                await AssignManagerPermissionsAsync(managerRole.Id, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Manager role not found. Manager user created without role assignment.");
            }

            _logger.LogInformation("Manager user created: {Username} ({Email})", managerUser.Username, managerUser.Email);
            return managerUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Manager user");
            return null;
        }
    }

    private async Task AssignManagerPermissionsAsync(Guid managerRoleId, CancellationToken cancellationToken)
    {
        try
        {
            var managerPermissionNames = new[]
            {
                // Events
                "Events.Events.Read", "Events.Events.Create", "Events.Events.Update",
                // Teams
                "Events.Teams.Read", "Events.Teams.Create", "Events.Teams.Update",
                // Products
                "Products.Products.Read", "Products.Products.Create", "Products.Products.Update",
                // Warehouse
                "Products.Warehouse.Read", "Products.Warehouse.Create", "Products.Warehouse.Update",
                // Documents
                "Documents.Documents.Read", "Documents.Documents.Create",
                // Sales
                "Sales.Sales.Read", "Sales.Sales.Create",
                // Reports
                "Reports.Reports.Read"
            };

            var allPermissions = await _dbContext.Permissions
                .Where(p => managerPermissionNames.Contains(p.Name))
                .ToListAsync(cancellationToken);

            var existingRolePermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == managerRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);

            var toAdd = new List<RolePermission>();
            foreach (var perm in allPermissions)
            {
                if (!existingRolePermissions.Contains(perm.Id))
                {
                    toAdd.Add(new RolePermission
                    {
                        RoleId = managerRoleId,
                        PermissionId = perm.Id,
                        GrantedBy = "system",
                        GrantedAt = DateTime.UtcNow,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow,
                        TenantId = Guid.Empty
                    });
                }
            }

            if (toAdd.Any())
            {
                _dbContext.RolePermissions.AddRange(toAdd);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Assigned {Count} permissions to Manager role", toAdd.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to Manager role");
        }
    }
}
