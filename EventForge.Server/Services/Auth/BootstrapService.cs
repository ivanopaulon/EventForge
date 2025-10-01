using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth;

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
    public string DefaultAdminUsername { get; set; } = "superadmin";

    /// <summary>
    /// Default admin email.
    /// </summary>
    public string DefaultAdminEmail { get; set; } = "superadmin@localhost";

    /// <summary>
    /// Default admin password.
    /// </summary>
    public string DefaultAdminPassword { get; set; } = "SuperAdmin#2025!";

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

        // Get password with environment variable -> config -> fallback precedence
        var envPassword = Environment.GetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD");
        var configPassword = configuration["Bootstrap:SuperAdminPassword"];
        var fallbackPassword = "SuperAdmin#2025!";

        _options = configuration.GetSection("Bootstrap").Get<BootstrapOptions>() ?? new BootstrapOptions();
        _options.DefaultAdminPassword = envPassword ?? configPassword ?? fallbackPassword;
    }

    public async Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting bootstrap process...");

            // Ensure database is created
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

            // Always seed/update default roles and permissions
            if (!await SeedDefaultRolesAndPermissionsAsync(cancellationToken))
            {
                _logger.LogError("Failed to seed default roles and permissions");
                return false;
            }

            // Always ensure SuperAdmin license is up to date
            var superAdminLicense = await EnsureSuperAdminLicenseAsync(cancellationToken);
            if (superAdminLicense == null)
            {
                _logger.LogError("Failed to ensure SuperAdmin license");
                return false;
            }

            // Check if any tenants exist
            var existingTenants = await _dbContext.Tenants.AnyAsync(cancellationToken);
            if (existingTenants)
            {
                _logger.LogInformation("Tenants already exist. Bootstrap data update completed.");
                return true;
            }

            _logger.LogInformation("No tenants found. Starting initial bootstrap...");

            // Create default tenant
            var defaultTenant = await CreateDefaultTenantAsync(cancellationToken);
            if (defaultTenant == null)
            {
                _logger.LogError("Failed to create default tenant");
                return false;
            }

            // Assign SuperAdmin license to default tenant
            if (!await AssignLicenseToTenantAsync(defaultTenant.Id, superAdminLicense.Id, cancellationToken))
            {
                _logger.LogError("Failed to assign SuperAdmin license to default tenant");
                return false;
            }

            // Create SuperAdmin user
            var superAdminUser = await CreateSuperAdminUserAsync(defaultTenant.Id, cancellationToken);
            if (superAdminUser == null)
            {
                _logger.LogError("Failed to create SuperAdmin user");
                return false;
            }

            // Create AdminTenant record
            if (!await CreateAdminTenantRecordAsync(superAdminUser.Id, defaultTenant.Id, cancellationToken))
            {
                _logger.LogError("Failed to create AdminTenant record");
                return false;
            }

            _logger.LogInformation("=== BOOTSTRAP COMPLETED SUCCESSFULLY ===");
            _logger.LogInformation("Default tenant created: {TenantName} (Code: {TenantCode})", defaultTenant.Name, defaultTenant.Code);
            _logger.LogInformation("SuperAdmin user created: {Username} ({Email})", superAdminUser.Username, superAdminUser.Email);
            _logger.LogInformation("Password: {Password}", _options.DefaultAdminPassword);
            _logger.LogWarning("SECURITY: Please change the SuperAdmin password immediately after first login!");
            _logger.LogInformation("SuperAdmin license assigned with unlimited users and API calls, including all features");
            _logger.LogInformation("==========================================");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bootstrap process");
            return false;
        }
    }

    /// <summary>
    /// Creates the default tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default tenant entity</returns>
    private async Task<Tenant?> CreateDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create default tenant with specific requirements
            var defaultTenant = new Tenant
            {
                Name = "DefaultTenant",
                Code = "default",
                DisplayName = "Default Tenant",
                Description = "Default tenant created during initial setup",
                ContactEmail = "superadmin@localhost",
                Domain = "localhost",
                MaxUsers = 10,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _dbContext.Tenants.Add(defaultTenant);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default tenant created: {TenantName} (Code: {TenantCode})", defaultTenant.Name, defaultTenant.Code);
            return defaultTenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default tenant");
            return null;
        }
    }

    /// <summary>
    /// Creates the SuperAdmin user with specific requirements.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the admin user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user entity</returns>
    private async Task<User?> CreateSuperAdminUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
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

            // Create SuperAdmin user with specific requirements
            var superAdminUser = new User
            {
                Username = "superadmin",
                Email = "superadmin@localhost",
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

            _dbContext.Users.Add(superAdminUser);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign SuperAdmin role
            var superAdminRole = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);

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

                _dbContext.UserRoles.Add(userRole);
                await _dbContext.SaveChangesAsync(cancellationToken);

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

    /// <summary>
    /// Ensures the SuperAdmin license exists and is up to date with the code-defined configuration.
    /// Creates the license if it doesn't exist, or updates it if the definition has changed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SuperAdmin license entity</returns>
    private async Task<License?> EnsureSuperAdminLicenseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Define the expected license configuration (source of truth)
            var expectedConfig = new
            {
                Name = "superadmin",
                DisplayName = "SuperAdmin License",
                Description = "SuperAdmin license with unlimited features for complete system management",
                MaxUsers = int.MaxValue,
                MaxApiCallsPerMonth = int.MaxValue,
                TierLevel = 5,
                IsActive = true
            };

            // Check if superadmin license already exists
            var existingLicense = await _dbContext.Licenses
                .FirstOrDefaultAsync(l => l.Name == expectedConfig.Name, cancellationToken);

            if (existingLicense != null)
            {
                // Update license if any properties differ from expected configuration
                var hasChanges = false;

                if (existingLicense.DisplayName != expectedConfig.DisplayName)
                {
                    _logger.LogInformation("Updating SuperAdmin license DisplayName: '{OldValue}' -> '{NewValue}'",
                        existingLicense.DisplayName, expectedConfig.DisplayName);
                    existingLicense.DisplayName = expectedConfig.DisplayName;
                    hasChanges = true;
                }

                if (existingLicense.Description != expectedConfig.Description)
                {
                    _logger.LogInformation("Updating SuperAdmin license Description");
                    existingLicense.Description = expectedConfig.Description;
                    hasChanges = true;
                }

                if (existingLicense.MaxUsers != expectedConfig.MaxUsers)
                {
                    _logger.LogInformation("Updating SuperAdmin license MaxUsers: {OldValue} -> {NewValue}",
                        existingLicense.MaxUsers, expectedConfig.MaxUsers);
                    existingLicense.MaxUsers = expectedConfig.MaxUsers;
                    hasChanges = true;
                }

                if (existingLicense.MaxApiCallsPerMonth != expectedConfig.MaxApiCallsPerMonth)
                {
                    _logger.LogInformation("Updating SuperAdmin license MaxApiCallsPerMonth: {OldValue} -> {NewValue}",
                        existingLicense.MaxApiCallsPerMonth, expectedConfig.MaxApiCallsPerMonth);
                    existingLicense.MaxApiCallsPerMonth = expectedConfig.MaxApiCallsPerMonth;
                    hasChanges = true;
                }

                if (existingLicense.TierLevel != expectedConfig.TierLevel)
                {
                    _logger.LogInformation("Updating SuperAdmin license TierLevel: {OldValue} -> {NewValue}",
                        existingLicense.TierLevel, expectedConfig.TierLevel);
                    existingLicense.TierLevel = expectedConfig.TierLevel;
                    hasChanges = true;
                }

                if (existingLicense.IsActive != expectedConfig.IsActive)
                {
                    _logger.LogInformation("Updating SuperAdmin license IsActive: {OldValue} -> {NewValue}",
                        existingLicense.IsActive, expectedConfig.IsActive);
                    existingLicense.IsActive = expectedConfig.IsActive;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    existingLicense.ModifiedBy = "system";
                    existingLicense.ModifiedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("SuperAdmin license updated with new configuration");
                }
                else
                {
                    _logger.LogInformation("SuperAdmin license is up to date");
                }

                // Always sync license features to ensure they match the expected configuration
                await SyncSuperAdminLicenseFeaturesAsync(existingLicense.Id, cancellationToken);

                return existingLicense;
            }

            // License doesn't exist, create it
            var superAdminLicense = new License
            {
                Name = expectedConfig.Name,
                DisplayName = expectedConfig.DisplayName,
                Description = expectedConfig.Description,
                MaxUsers = expectedConfig.MaxUsers,
                MaxApiCallsPerMonth = expectedConfig.MaxApiCallsPerMonth,
                TierLevel = expectedConfig.TierLevel,
                IsActive = expectedConfig.IsActive,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _dbContext.Licenses.Add(superAdminLicense);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SuperAdmin license created: {LicenseName}", superAdminLicense.Name);

            // Create all license features for SuperAdmin
            await SyncSuperAdminLicenseFeaturesAsync(superAdminLicense.Id, cancellationToken);

            return superAdminLicense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring SuperAdmin license");
            return null;
        }
    }

    /// <summary>
    /// Synchronizes all license features for the SuperAdmin license with the code-defined configuration.
    /// Adds new features, updates existing ones, and marks obsolete ones as inactive.
    /// </summary>
    /// <param name="licenseId">License ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    private async Task SyncSuperAdminLicenseFeaturesAsync(Guid licenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Define the expected features configuration (source of truth)
            var expectedFeatures = new[]
            {
                // Event Management
                new { Name = "BasicEventManagement", DisplayName = "Gestione Eventi Base", Description = "Funzionalità base per la gestione degli eventi", Category = "Events" },
                
                // Team Management
                new { Name = "BasicTeamManagement", DisplayName = "Gestione Team Base", Description = "Funzionalità base per la gestione dei team", Category = "Teams" },
                
                // Product & Warehouse Management
                new { Name = "ProductManagement", DisplayName = "Gestione Prodotti", Description = "Funzionalità complete per la gestione dei prodotti e magazzino", Category = "Products" },
                
                // Document Management
                new { Name = "DocumentManagement", DisplayName = "Gestione Documenti", Description = "Funzionalità complete per la gestione documenti, ricorrenze e riferimenti", Category = "Documents" },
                
                // Financial Management
                new { Name = "FinancialManagement", DisplayName = "Gestione Finanziaria", Description = "Gestione banche, termini di pagamento e aliquote IVA", Category = "Financial" },
                
                // Entity Management
                new { Name = "EntityManagement", DisplayName = "Gestione Entità", Description = "Gestione indirizzi, contatti e nodi di classificazione", Category = "Entities" },
                
                // Reporting
                new { Name = "BasicReporting", DisplayName = "Report Base", Description = "Funzionalità di reporting standard", Category = "Reports" },
                new { Name = "AdvancedReporting", DisplayName = "Report Avanzati", Description = "Funzionalità di reporting avanzate e analisi", Category = "Reports" },
                
                // Communication
                new { Name = "ChatManagement", DisplayName = "Gestione Chat", Description = "Funzionalità di chat e messaggistica", Category = "Communication" },
                new { Name = "NotificationManagement", DisplayName = "Gestione Notifiche", Description = "Funzionalità avanzate per le notifiche", Category = "Communication" },
                
                // Retail & POS
                new { Name = "RetailManagement", DisplayName = "Gestione Retail", Description = "Gestione punto vendita, carrelli e stazioni", Category = "Retail" },
                new { Name = "StoreManagement", DisplayName = "Gestione Negozi", Description = "Gestione negozi e utenti punto vendita", Category = "Retail" },
                
                // Printing
                new { Name = "PrintingManagement", DisplayName = "Gestione Stampa", Description = "Funzionalità di stampa e gestione etichette", Category = "Printing" },
                
                // Integrations
                new { Name = "ApiIntegrations", DisplayName = "Integrazioni API", Description = "Accesso completo alle API per integrazioni esterne", Category = "Integrations" },
                new { Name = "CustomIntegrations", DisplayName = "Integrazioni Custom", Description = "Integrazioni personalizzate e webhook", Category = "Integrations" },
                
                // Security
                new { Name = "AdvancedSecurity", DisplayName = "Sicurezza Avanzata", Description = "Funzionalità di sicurezza avanzate", Category = "Security" }
            };

            // Get existing features for this license
            var existingFeatures = await _dbContext.LicenseFeatures
                .Where(lf => lf.LicenseId == licenseId)
                .ToListAsync(cancellationToken);

            var featuresAdded = 0;
            var featuresUpdated = 0;

            // Process each expected feature
            foreach (var expected in expectedFeatures)
            {
                var existing = existingFeatures.FirstOrDefault(f => f.Name == expected.Name);

                if (existing == null)
                {
                    // Feature doesn't exist, create it
                    var newFeature = new LicenseFeature
                    {
                        Name = expected.Name,
                        DisplayName = expected.DisplayName,
                        Description = expected.Description,
                        Category = expected.Category,
                        LicenseId = licenseId,
                        IsActive = true,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow,
                        TenantId = Guid.Empty
                    };

                    _dbContext.LicenseFeatures.Add(newFeature);
                    featuresAdded++;
                    _logger.LogInformation("Adding new SuperAdmin license feature: {FeatureName}", expected.Name);
                }
                else
                {
                    // Feature exists, check if it needs updating
                    var hasChanges = false;

                    if (existing.DisplayName != expected.DisplayName)
                    {
                        existing.DisplayName = expected.DisplayName;
                        hasChanges = true;
                    }

                    if (existing.Description != expected.Description)
                    {
                        existing.Description = expected.Description;
                        hasChanges = true;
                    }

                    if (existing.Category != expected.Category)
                    {
                        existing.Category = expected.Category;
                        hasChanges = true;
                    }

                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        existing.ModifiedBy = "system";
                        existing.ModifiedAt = DateTime.UtcNow;
                        featuresUpdated++;
                        _logger.LogInformation("Updating SuperAdmin license feature: {FeatureName}", expected.Name);
                    }
                }
            }

            // Mark features that are no longer in the expected list as inactive
            var expectedNames = expectedFeatures.Select(f => f.Name).ToHashSet();
            var obsoleteFeatures = existingFeatures.Where(f => !expectedNames.Contains(f.Name) && f.IsActive).ToList();

            foreach (var obsolete in obsoleteFeatures)
            {
                obsolete.IsActive = false;
                obsolete.ModifiedBy = "system";
                obsolete.ModifiedAt = DateTime.UtcNow;
                _logger.LogInformation("Marking obsolete SuperAdmin license feature as inactive: {FeatureName}", obsolete.Name);
            }

            if (featuresAdded > 0 || featuresUpdated > 0 || obsoleteFeatures.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("SuperAdmin license features synchronized: {Added} added, {Updated} updated, {Deactivated} deactivated",
                    featuresAdded, featuresUpdated, obsoleteFeatures.Count);
            }
            else
            {
                _logger.LogInformation("SuperAdmin license features are up to date");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing SuperAdmin license features");
            throw;
        }
    }

    /// <summary>
    /// Assigns a license to a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="licenseId">License ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> AssignLicenseToTenantAsync(Guid tenantId, Guid licenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantLicense = new TenantLicense
            {
                TargetTenantId = tenantId,
                LicenseId = licenseId,
                StartsAt = DateTime.UtcNow,
                IsAssignmentActive = true,
                ApiCallsThisMonth = 0,
                ApiCallsResetAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _dbContext.TenantLicenses.Add(tenantLicense);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Basic license assigned to tenant: {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning license to tenant");
            return false;
        }
    }

    /// <summary>
    /// Creates an AdminTenant record granting SuperAdmin access to manage the default tenant.
    /// </summary>
    /// <param name="userId">SuperAdmin user ID</param>
    /// <param name="tenantId">Tenant ID to manage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> CreateAdminTenantRecordAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminTenant = new AdminTenant
            {
                UserId = userId,
                ManagedTenantId = tenantId,
                AccessLevel = AdminAccessLevel.FullAccess,
                GrantedAt = DateTime.UtcNow,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _dbContext.AdminTenants.Add(adminTenant);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AdminTenant record created for user {UserId} to manage tenant {TenantId}", userId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AdminTenant record");
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
                new Role { Name = "SuperAdmin", DisplayName = "Super Administrator", Description = "Full unrestricted system access", IsSystemRole = true },
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