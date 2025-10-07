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
            _ = await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

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

            // Seed base entities for the tenant
            if (!await SeedTenantBaseEntitiesAsync(defaultTenant.Id, cancellationToken))
            {
                _logger.LogError("Failed to seed base entities for tenant");
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

            _ = _dbContext.Tenants.Add(defaultTenant);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

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

            _ = _dbContext.Users.Add(superAdminUser);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

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
                    _ = await _dbContext.SaveChangesAsync(cancellationToken);
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

            _ = _dbContext.Licenses.Add(superAdminLicense);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

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

                    _ = _dbContext.LicenseFeatures.Add(newFeature);
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
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
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

            _ = _dbContext.TenantLicenses.Add(tenantLicense);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

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

            _ = _dbContext.AdminTenants.Add(adminTenant);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

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
                new Permission { Name = "Users.Users.Create", DisplayName = "Create Users", Category = "Users", Resource = "Users", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Users.Read", DisplayName = "View Users", Category = "Users", Resource = "Users", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Users.Update", DisplayName = "Update Users", Category = "Users", Resource = "Users", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Users.Delete", DisplayName = "Delete Users", Category = "Users", Resource = "Users", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Role management
                new Permission { Name = "Users.Roles.Create", DisplayName = "Create Roles", Category = "Users", Resource = "Roles", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Roles.Read", DisplayName = "View Roles", Category = "Users", Resource = "Roles", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Roles.Update", DisplayName = "Update Roles", Category = "Users", Resource = "Roles", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Users.Roles.Delete", DisplayName = "Delete Roles", Category = "Users", Resource = "Roles", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Events
                new Permission { Name = "Events.Events.Create", DisplayName = "Create Events", Category = "Events", Resource = "Events", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Events.Read", DisplayName = "View Events", Category = "Events", Resource = "Events", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Events.Update", DisplayName = "Update Events", Category = "Events", Resource = "Events", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Events.Delete", DisplayName = "Delete Events", Category = "Events", Resource = "Events", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Teams
                new Permission { Name = "Events.Teams.Create", DisplayName = "Create Teams", Category = "Events", Resource = "Teams", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Teams.Read", DisplayName = "View Teams", Category = "Events", Resource = "Teams", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Teams.Update", DisplayName = "Update Teams", Category = "Events", Resource = "Teams", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Events.Teams.Delete", DisplayName = "Delete Teams", Category = "Events", Resource = "Teams", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Products
                new Permission { Name = "Products.Products.Create", DisplayName = "Create Products", Category = "Products", Resource = "Products", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Products.Read", DisplayName = "View Products", Category = "Products", Resource = "Products", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Products.Update", DisplayName = "Update Products", Category = "Products", Resource = "Products", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Products.Delete", DisplayName = "Delete Products", Category = "Products", Resource = "Products", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Warehouse
                new Permission { Name = "Products.Warehouse.Create", DisplayName = "Create Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Warehouse.Read", DisplayName = "View Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Warehouse.Update", DisplayName = "Update Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Products.Warehouse.Delete", DisplayName = "Delete Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Documents
                new Permission { Name = "Documents.Documents.Create", DisplayName = "Create Documents", Category = "Documents", Resource = "Documents", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Documents.Documents.Read", DisplayName = "View Documents", Category = "Documents", Resource = "Documents", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Documents.Documents.Update", DisplayName = "Update Documents", Category = "Documents", Resource = "Documents", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Documents.Documents.Delete", DisplayName = "Delete Documents", Category = "Documents", Resource = "Documents", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Financial
                new Permission { Name = "Financial.Banks.Create", DisplayName = "Create Banks", Category = "Financial", Resource = "Banks", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Financial.Banks.Read", DisplayName = "View Banks", Category = "Financial", Resource = "Banks", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Financial.Banks.Update", DisplayName = "Update Banks", Category = "Financial", Resource = "Banks", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Financial.Banks.Delete", DisplayName = "Delete Banks", Category = "Financial", Resource = "Banks", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Sales
                new Permission { Name = "Sales.Sales.Create", DisplayName = "Create Sales", Category = "Sales", Resource = "Sales", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Sales.Read", DisplayName = "View Sales", Category = "Sales", Resource = "Sales", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Sales.Update", DisplayName = "Update Sales", Category = "Sales", Resource = "Sales", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Sales.Delete", DisplayName = "Delete Sales", Category = "Sales", Resource = "Sales", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Tables
                new Permission { Name = "Sales.Tables.Create", DisplayName = "Create Tables", Category = "Sales", Resource = "Tables", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Tables.Read", DisplayName = "View Tables", Category = "Sales", Resource = "Tables", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Tables.Update", DisplayName = "Update Tables", Category = "Sales", Resource = "Tables", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.Tables.Delete", DisplayName = "Delete Tables", Category = "Sales", Resource = "Tables", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Payment Methods
                new Permission { Name = "Sales.PaymentMethods.Create", DisplayName = "Create Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.PaymentMethods.Read", DisplayName = "View Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.PaymentMethods.Update", DisplayName = "Update Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Sales.PaymentMethods.Delete", DisplayName = "Delete Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Notifications
                new Permission { Name = "Communication.Notifications.Create", DisplayName = "Create Notifications", Category = "Communication", Resource = "Notifications", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Notifications.Read", DisplayName = "View Notifications", Category = "Communication", Resource = "Notifications", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Notifications.Update", DisplayName = "Update Notifications", Category = "Communication", Resource = "Notifications", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Notifications.Delete", DisplayName = "Delete Notifications", Category = "Communication", Resource = "Notifications", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Chat
                new Permission { Name = "Communication.Chat.Create", DisplayName = "Create Chat Messages", Category = "Communication", Resource = "Chat", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Chat.Read", DisplayName = "View Chat Messages", Category = "Communication", Resource = "Chat", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Chat.Update", DisplayName = "Update Chat Messages", Category = "Communication", Resource = "Chat", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Communication.Chat.Delete", DisplayName = "Delete Chat Messages", Category = "Communication", Resource = "Chat", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Retail
                new Permission { Name = "Retail.Carts.Create", DisplayName = "Create Retail Carts", Category = "Retail", Resource = "Carts", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Carts.Read", DisplayName = "View Retail Carts", Category = "Retail", Resource = "Carts", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Carts.Update", DisplayName = "Update Retail Carts", Category = "Retail", Resource = "Carts", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Carts.Delete", DisplayName = "Delete Retail Carts", Category = "Retail", Resource = "Carts", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Stores
                new Permission { Name = "Retail.Stores.Create", DisplayName = "Create Stores", Category = "Retail", Resource = "Stores", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Stores.Read", DisplayName = "View Stores", Category = "Retail", Resource = "Stores", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Stores.Update", DisplayName = "Update Stores", Category = "Retail", Resource = "Stores", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Retail.Stores.Delete", DisplayName = "Delete Stores", Category = "Retail", Resource = "Stores", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Printing
                new Permission { Name = "Printing.Print.Create", DisplayName = "Create Print Jobs", Category = "Printing", Resource = "Print", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Printing.Print.Read", DisplayName = "View Print Jobs", Category = "Printing", Resource = "Print", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },

                // Entities
                new Permission { Name = "Entities.Entities.Create", DisplayName = "Create Entities", Category = "Entities", Resource = "Entities", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Entities.Entities.Read", DisplayName = "View Entities", Category = "Entities", Resource = "Entities", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Entities.Entities.Update", DisplayName = "Update Entities", Category = "Entities", Resource = "Entities", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Entities.Entities.Delete", DisplayName = "Delete Entities", Category = "Entities", Resource = "Entities", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

                // Reports
                new Permission { Name = "Reports.Reports.Read", DisplayName = "View Reports", Category = "Reports", Resource = "Reports", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "Reports.Audit.Read", DisplayName = "View Audit Logs", Category = "Reports", Resource = "Audit", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },

                // System
                new Permission { Name = "System.Settings.Update", DisplayName = "Update System Settings", Category = "System", Resource = "Settings", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
                new Permission { Name = "System.Logs.Read", DisplayName = "View System Logs", Category = "System", Resource = "Logs", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty }
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
                    _ = _dbContext.Permissions.Add(permission);
                }
            }

            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Define default roles
            var defaultRoles = new[]
            {
                new Role { Name = "SuperAdmin", DisplayName = "Super Administrator", Description = "Full unrestricted system access", IsSystemRole = true, TenantId = Guid.Empty },
                new Role { Name = "Admin", DisplayName = "System Administrator", Description = "Full system access", IsSystemRole = true, TenantId = Guid.Empty },
                new Role { Name = "Manager", DisplayName = "Manager", Description = "Management level access", IsSystemRole = true, TenantId = Guid.Empty },
                new Role { Name = "User", DisplayName = "Standard User", Description = "Basic user access", IsSystemRole = true, TenantId = Guid.Empty },
                new Role { Name = "Viewer", DisplayName = "Viewer", Description = "Read-only access", IsSystemRole = true, TenantId = Guid.Empty }
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
                    _ = _dbContext.Roles.Add(role);
                }
            }

            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Get all permissions once for assigning to roles
            var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

            // Assign permissions to Admin role
            var adminRole = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

            if (adminRole != null && !adminRole.RolePermissions.Any())
            {
                foreach (var permission in allPermissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id,
                        GrantedBy = "system",
                        GrantedAt = DateTime.UtcNow,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow,
                        TenantId = Guid.Empty
                    };

                    _ = _dbContext.RolePermissions.Add(rolePermission);
                }

                _logger.LogInformation("Assigned {Count} permissions to Admin role", allPermissions.Count);
            }

            // Assign permissions to SuperAdmin role - SuperAdmin must have unrestricted access to everything
            var superAdminRole = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);

            if (superAdminRole != null && !superAdminRole.RolePermissions.Any())
            {
                foreach (var permission in allPermissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = superAdminRole.Id,
                        PermissionId = permission.Id,
                        GrantedBy = "system",
                        GrantedAt = DateTime.UtcNow,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow,
                        TenantId = Guid.Empty
                    };

                    _ = _dbContext.RolePermissions.Add(rolePermission);
                }

                _logger.LogInformation("Assigned {Count} permissions to SuperAdmin role", allPermissions.Count);
            }

            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default roles and permissions seeded successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default roles and permissions");
            return false;
        }
    }

    /// <summary>
    /// Seeds base entities for a tenant (warehouse, storage location, VAT rates, VAT natures, units of measure).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedTenantBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding base entities for tenant {TenantId}...", tenantId);

            // Seed VAT natures first (needed for VAT rates)
            if (!await SeedVatNaturesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed VAT natures");
                return false;
            }

            // Seed VAT rates
            if (!await SeedVatRatesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed VAT rates");
                return false;
            }

            // Seed units of measure
            if (!await SeedUnitsOfMeasureAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed units of measure");
                return false;
            }

            // Seed default warehouse and storage location
            if (!await SeedDefaultWarehouseAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed default warehouse");
                return false;
            }

            // Seed document types
            if (!await SeedDocumentTypesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed document types");
                return false;
            }

            _logger.LogInformation("Base entities seeded successfully for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding base entities for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds Italian VAT nature codes (Natura IVA).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedVatNaturesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT natures for tenant {TenantId}...", tenantId);

            // Check if VAT natures already exist for this tenant
            var existingNatures = await _dbContext.VatNatures
                .AnyAsync(v => v.TenantId == tenantId, cancellationToken);

            if (existingNatures)
            {
                _logger.LogInformation("VAT natures already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Italian VAT nature codes as per current legislation
            var vatNatures = new[]
            {
                new VatNature
                {
                    Code = "N1",
                    Name = "Escluse ex art. 15",
                    Description = "Operazioni escluse dal campo di applicazione dell'IVA ex art. 15 del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2",
                    Name = "Non soggette",
                    Description = "Operazioni non soggette ad IVA ai sensi degli artt. da 7 a 7-septies del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2.1",
                    Name = "Non soggette ad IVA ai sensi degli artt. da 7 a 7-septies",
                    Description = "Operazioni non soggette - Cessioni di beni e prestazioni di servizi non soggette per carenza del presupposto territoriale",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2.2",
                    Name = "Non soggette ad IVA - Altre operazioni",
                    Description = "Operazioni non soggette - Altre operazioni che non configurano una cessione di beni né una prestazione di servizi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3",
                    Name = "Non imponibili",
                    Description = "Operazioni non imponibili (esportazioni, cessioni intracomunitarie, ecc.)",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.1",
                    Name = "Non imponibili - Esportazioni",
                    Description = "Esportazioni di cui agli artt. 8 e 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.2",
                    Name = "Non imponibili - Cessioni intracomunitarie",
                    Description = "Cessioni intracomunitarie di cui all'art. 41 del DL 331/93",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.3",
                    Name = "Non imponibili - Cessioni verso San Marino",
                    Description = "Cessioni verso San Marino di cui all'art. 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.4",
                    Name = "Non imponibili - Operazioni assimilate",
                    Description = "Operazioni assimilate alle cessioni all'esportazione di cui all'art. 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.5",
                    Name = "Non imponibili - Altre operazioni",
                    Description = "Operazioni non imponibili a seguito di dichiarazioni d'intento",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.6",
                    Name = "Non imponibili - Altre operazioni non imponibili",
                    Description = "Altre operazioni non imponibili che non concorrono alla formazione del plafond",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N4",
                    Name = "Esenti",
                    Description = "Operazioni esenti da IVA ai sensi degli artt. 10 del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N5",
                    Name = "Regime del margine",
                    Description = "Regime del margine / IVA non esposta in fattura ai sensi dell'art. 36 del DL 41/95",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6",
                    Name = "Inversione contabile",
                    Description = "Inversione contabile (reverse charge) per cessioni di rottami, altri materiali, subappalti, ecc.",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.1",
                    Name = "Inversione contabile - Cessioni di rottami",
                    Description = "Inversione contabile - Cessione di rottami e altri materiali di recupero",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.2",
                    Name = "Inversione contabile - Cessioni di oro e argento",
                    Description = "Inversione contabile - Cessione di oro e argento puro",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.3",
                    Name = "Inversione contabile - Subappalto",
                    Description = "Inversione contabile - Subappalto nel settore edile",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.4",
                    Name = "Inversione contabile - Cessioni di fabbricati",
                    Description = "Inversione contabile - Cessioni di fabbricati",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.5",
                    Name = "Inversione contabile - Cessioni di telefoni cellulari",
                    Description = "Inversione contabile - Cessioni di telefoni cellulari",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.6",
                    Name = "Inversione contabile - Cessioni di prodotti elettronici",
                    Description = "Inversione contabile - Cessioni di prodotti elettronici",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.7",
                    Name = "Inversione contabile - Prestazioni settore edile",
                    Description = "Inversione contabile - Prestazioni comparto edile e settori connessi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.8",
                    Name = "Inversione contabile - Operazioni settore energetico",
                    Description = "Inversione contabile - Operazioni settore energetico",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.9",
                    Name = "Inversione contabile - Altri casi",
                    Description = "Inversione contabile - Altri casi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N7",
                    Name = "IVA assolta in altro stato UE",
                    Description = "IVA assolta in altro stato UE (vendite a distanza ex art. 40 c. 3 e 4 e art. 41 c. 1 lett. b, DL 331/93)",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.VatNatures.AddRange(vatNatures);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} VAT natures for tenant {TenantId}", vatNatures.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding VAT natures for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds Italian VAT rates (current legislation).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedVatRatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT rates for tenant {TenantId}...", tenantId);

            // Check if VAT rates already exist for this tenant
            var existingRates = await _dbContext.VatRates
                .AnyAsync(v => v.TenantId == tenantId, cancellationToken);

            if (existingRates)
            {
                _logger.LogInformation("VAT rates already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Italian VAT rates as per current legislation (2024-2025)
            var vatRates = new[]
            {
                new VatRate
                {
                    Name = "IVA 22%",
                    Percentage = 22m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ordinaria",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 10%",
                    Percentage = 10m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ridotta - Generi alimentari, bevande, servizi turistici",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 5%",
                    Percentage = 5m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ridotta - Generi di prima necessità",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 4%",
                    Percentage = 4m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA minima - Generi di prima necessità (pane, latte, ecc.)",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 0%",
                    Percentage = 0m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Operazioni non imponibili, esenti o fuori campo IVA",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.VatRates.AddRange(vatRates);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} VAT rates for tenant {TenantId}", vatRates.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding VAT rates for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds common units of measure for warehouse management.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedUnitsOfMeasureAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding units of measure for tenant {TenantId}...", tenantId);

            // Check if units of measure already exist for this tenant
            var existingUnits = await _dbContext.UMs
                .AnyAsync(u => u.TenantId == tenantId, cancellationToken);

            if (existingUnits)
            {
                _logger.LogInformation("Units of measure already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Common units of measure for warehouse management
            var unitsOfMeasure = new[]
            {
                // Count/Piece units
                new UM
                {
                    Name = "Pezzo",
                    Symbol = "pz",
                    Description = "Unità di misura per pezzi singoli",
                    IsDefault = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Confezione",
                    Symbol = "conf",
                    Description = "Unità di misura per confezioni",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Scatola",
                    Symbol = "scat",
                    Description = "Unità di misura per scatole",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Cartone",
                    Symbol = "cart",
                    Description = "Unità di misura per cartoni",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Pallet",
                    Symbol = "pallet",
                    Description = "Unità di misura per pallet",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Bancale",
                    Symbol = "banc",
                    Description = "Unità di misura per bancali",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Collo",
                    Symbol = "collo",
                    Description = "Unità di misura per colli",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Weight units
                new UM
                {
                    Name = "Kilogrammo",
                    Symbol = "kg",
                    Description = "Unità di misura di peso - chilogrammi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Grammo",
                    Symbol = "g",
                    Description = "Unità di misura di peso - grammi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Tonnellata",
                    Symbol = "t",
                    Description = "Unità di misura di peso - tonnellate",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Quintale",
                    Symbol = "q",
                    Description = "Unità di misura di peso - quintali",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Volume units
                new UM
                {
                    Name = "Litro",
                    Symbol = "l",
                    Description = "Unità di misura di volume - litri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Millilitro",
                    Symbol = "ml",
                    Description = "Unità di misura di volume - millilitri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Metro cubo",
                    Symbol = "m³",
                    Description = "Unità di misura di volume - metri cubi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Length units
                new UM
                {
                    Name = "Metro",
                    Symbol = "m",
                    Description = "Unità di misura di lunghezza - metri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Centimetro",
                    Symbol = "cm",
                    Description = "Unità di misura di lunghezza - centimetri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Metro quadrato",
                    Symbol = "m²",
                    Description = "Unità di misura di superficie - metri quadrati",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Other units
                new UM
                {
                    Name = "Paio",
                    Symbol = "paio",
                    Description = "Unità di misura per paia",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Set",
                    Symbol = "set",
                    Description = "Unità di misura per set",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Kit",
                    Symbol = "kit",
                    Description = "Unità di misura per kit",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.UMs.AddRange(unitsOfMeasure);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} units of measure for tenant {TenantId}", unitsOfMeasure.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding units of measure for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds a default warehouse and storage location for the tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedDefaultWarehouseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding default warehouse for tenant {TenantId}...", tenantId);

            // Check if warehouses already exist for this tenant
            var existingWarehouses = await _dbContext.StorageFacilities
                .AnyAsync(w => w.TenantId == tenantId, cancellationToken);

            if (existingWarehouses)
            {
                _logger.LogInformation("Warehouses already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Create default warehouse
            var defaultWarehouse = new StorageFacility
            {
                Name = "Magazzino Principale",
                Code = "MAG-01",
                Address = "Indirizzo da completare",
                Notes = "Magazzino principale creato durante l'inizializzazione del sistema",
                IsFiscal = true,
                IsActive = true,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _dbContext.StorageFacilities.Add(defaultWarehouse);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Create default storage location
            var defaultLocation = new StorageLocation
            {
                Code = "UB-DEF",
                Description = "Ubicazione predefinita",
                WarehouseId = defaultWarehouse.Id,
                IsActive = true,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _dbContext.StorageLocations.Add(defaultLocation);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created default warehouse '{WarehouseName}' with default location '{LocationCode}' for tenant {TenantId}",
                defaultWarehouse.Name, defaultLocation.Code, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default warehouse for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds standard document types for the tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    private async Task<bool> SeedDocumentTypesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding document types for tenant {TenantId}...", tenantId);

            // Check if document types already exist for this tenant
            var existingDocTypes = await _dbContext.DocumentTypes
                .AnyAsync(dt => dt.TenantId == tenantId, cancellationToken);

            if (existingDocTypes)
            {
                _logger.LogInformation("Document types already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Get the default warehouse for document type configuration
            var defaultWarehouse = await _dbContext.StorageFacilities
                .FirstOrDefaultAsync(w => w.TenantId == tenantId, cancellationToken);

            // Standard document types for Italian businesses
            var documentTypes = new[]
            {
                // Inventory Document
                new DocumentType
                {
                    Code = "INVENTORY",
                    Name = "Documento di Inventario",
                    Notes = "Documento per la rilevazione fisica dell'inventario di magazzino",
                    IsStockIncrease = false, // Inventory adjustments can go either way
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Delivery Note
                new DocumentType
                {
                    Code = "DDT_VEND",
                    Name = "Bolla di Vendita (DDT)",
                    Notes = "Documento di trasporto per vendita - riduce giacenza magazzino",
                    IsStockIncrease = false, // Delivery decreases stock
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Delivery Note
                new DocumentType
                {
                    Code = "DDT_ACQ",
                    Name = "Bolla di Acquisto (DDT)",
                    Notes = "Documento di trasporto per acquisto - aumenta giacenza magazzino",
                    IsStockIncrease = true, // Purchase increases stock
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Transfer Note
                new DocumentType
                {
                    Code = "DDT_TRASF",
                    Name = "Bolla di Trasferimento",
                    Notes = "Documento di trasporto per trasferimento tra magazzini",
                    IsStockIncrease = false, // Transfer is neutral (reduces source, increases destination)
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Invoice
                new DocumentType
                {
                    Code = "FATT_VEND",
                    Name = "Fattura di Vendita",
                    Notes = "Fattura di vendita - riduce giacenza magazzino se non già movimentata",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Invoice
                new DocumentType
                {
                    Code = "FATT_ACQ",
                    Name = "Fattura di Acquisto",
                    Notes = "Fattura di acquisto - aumenta giacenza magazzino se non già movimentata",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Receipt
                new DocumentType
                {
                    Code = "SCONTRINO",
                    Name = "Scontrino di Vendita",
                    Notes = "Scontrino fiscale per vendita al dettaglio - riduce giacenza magazzino",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Order
                new DocumentType
                {
                    Code = "ORD_VEND",
                    Name = "Ordine di Vendita",
                    Notes = "Ordine cliente - non movimenta giacenza fino all'evasione",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Order
                new DocumentType
                {
                    Code = "ORD_ACQ",
                    Name = "Ordine di Acquisto",
                    Notes = "Ordine fornitore - non movimenta giacenza fino al ricevimento",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Quote
                new DocumentType
                {
                    Code = "PREVENTIVO",
                    Name = "Preventivo",
                    Notes = "Preventivo/offerta cliente - non movimenta giacenza",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Return Note
                new DocumentType
                {
                    Code = "RESO",
                    Name = "Reso da Cliente",
                    Notes = "Documento per resi da cliente - aumenta giacenza magazzino",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Credit Note
                new DocumentType
                {
                    Code = "NOTA_CRED",
                    Name = "Nota di Credito",
                    Notes = "Nota di credito - può aumentare giacenza in caso di reso",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.DocumentTypes.AddRange(documentTypes);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} document types for tenant {TenantId}", documentTypes.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding document types for tenant {TenantId}", tenantId);
            return false;
        }
    }
}