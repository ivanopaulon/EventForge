using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of user seeding service.
/// </summary>
public class UserSeeder(
    EventForgeDbContext dbContext,
    IPasswordService passwordService,
    IConfiguration configuration,
    ILogger<UserSeeder> logger) : IUserSeeder
{

    private readonly BootstrapOptions _options = BuildOptions(configuration);

    private readonly string _managerPassword = BuildManagerPassword(configuration);

    public async Task<User?> CreateSuperAdminUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == _options.DefaultAdminUsername || u.Email == _options.DefaultAdminEmail, cancellationToken);
            if (existing is not null)
                return existing;

            var validation = passwordService.ValidatePassword(_options.DefaultAdminPassword);
            if (!validation.IsValid)
            {
                logger.LogError("SuperAdmin password does not meet policy requirements: {Errors}",
                    string.Join(", ", validation.Errors));
                return null;
            }

            // Hash password
            var (hash, salt) = passwordService.HashPassword(_options.DefaultAdminPassword);

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

            _ = dbContext.Users.Add(superAdminUser);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            var superAdminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);
            if (superAdminRole is not null)
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

                _ = dbContext.UserRoles.Add(userRole);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                logger.LogWarning("SuperAdmin role not found. User created without role assignment.");
            }

            return superAdminUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating SuperAdmin user");
            return null;
        }
    }

    public async Task<User?> CreateDefaultManagerUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string managerUsername = "manager";
        const string managerEmail = "manager@localhost";

        try
        {
            var existing = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == managerUsername || u.Email == managerEmail, cancellationToken);
            if (existing is not null)
                return existing;

            var validation = passwordService.ValidatePassword(_managerPassword);
            if (!validation.IsValid)
                logger.LogWarning("Default manager password does not meet policy requirements: {Errors}",
                    string.Join(", ", validation.Errors));

            var (hash, salt) = passwordService.HashPassword(_managerPassword);

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

            _ = dbContext.Users.Add(managerUser);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            var managerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Manager", cancellationToken);
            if (managerRole is not null)
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

                _ = dbContext.UserRoles.Add(userRole);
                _ = await dbContext.SaveChangesAsync(cancellationToken);

                await AssignManagerPermissionsAsync(managerRole.Id, cancellationToken);
            }
            else
            {
                logger.LogWarning("Manager role not found. Manager user created without role assignment.");
            }

            return managerUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Manager user");
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

            var allPermissions = await dbContext.Permissions
                .Where(p => managerPermissionNames.Contains(p.Name))
                .ToListAsync(cancellationToken);

            var existingRolePermissions = await dbContext.RolePermissions
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
                dbContext.RolePermissions.AddRange(toAdd);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning permissions to Manager role");
        }
    }

    private static string BuildManagerPassword(IConfiguration configuration)
    {

        var envManagerPassword = Environment.GetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_MANAGER_PASSWORD");
        return envManagerPassword
            ?? configuration["Bootstrap:DefaultManagerPassword"]
            ?? "Manager@2025!";
    }

    private static BootstrapOptions BuildOptions(IConfiguration configuration)
    {

        var envPassword = Environment.GetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD");

        var configPassword = configuration["Bootstrap:SuperAdminPassword"];
        var options = configuration.GetSection("Bootstrap").Get<BootstrapOptions>() ?? new BootstrapOptions();

        options.DefaultAdminPassword = envPassword ?? configPassword ?? "SuperAdmin#2025!";
        return options;
    }

}
