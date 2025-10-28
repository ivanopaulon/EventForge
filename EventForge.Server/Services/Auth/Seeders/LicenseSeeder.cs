using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of license seeding service.
/// </summary>
public class LicenseSeeder : ILicenseSeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<LicenseSeeder> _logger;

    public LicenseSeeder(
        EventForgeDbContext dbContext,
        ILogger<LicenseSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<License?> EnsureSuperAdminLicenseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
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

            var existingLicense = await _dbContext.Licenses
                .FirstOrDefaultAsync(l => l.Name == expectedConfig.Name, cancellationToken);

            if (existingLicense != null)
            {
                var hasChanges = false;

                if (existingLicense.DisplayName != expectedConfig.DisplayName)
                {
                    existingLicense.DisplayName = expectedConfig.DisplayName;
                    hasChanges = true;
                }

                if (existingLicense.Description != expectedConfig.Description)
                {
                    existingLicense.Description = expectedConfig.Description;
                    hasChanges = true;
                }

                if (existingLicense.MaxUsers != expectedConfig.MaxUsers)
                {
                    existingLicense.MaxUsers = expectedConfig.MaxUsers;
                    hasChanges = true;
                }

                if (existingLicense.MaxApiCallsPerMonth != expectedConfig.MaxApiCallsPerMonth)
                {
                    existingLicense.MaxApiCallsPerMonth = expectedConfig.MaxApiCallsPerMonth;
                    hasChanges = true;
                }

                if (existingLicense.TierLevel != expectedConfig.TierLevel)
                {
                    existingLicense.TierLevel = expectedConfig.TierLevel;
                    hasChanges = true;
                }

                if (existingLicense.IsActive != expectedConfig.IsActive)
                {
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

                // Always sync license features
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

            // Create all license features
            await SyncSuperAdminLicenseFeaturesAsync(superAdminLicense.Id, cancellationToken);

            return superAdminLicense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring SuperAdmin license");
            return null;
        }
    }

    public async Task<bool> AssignLicenseToTenantAsync(Guid tenantId, Guid licenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if license is already assigned to this tenant
            var existingAssignment = await _dbContext.TenantLicenses
                .FirstOrDefaultAsync(tl => tl.TargetTenantId == tenantId && tl.LicenseId == licenseId, cancellationToken);
            
            if (existingAssignment != null)
            {
                _logger.LogInformation("License {LicenseId} is already assigned to tenant {TenantId}", licenseId, tenantId);
                
                // Ensure assignment is active
                if (!existingAssignment.IsAssignmentActive)
                {
                    existingAssignment.IsAssignmentActive = true;
                    existingAssignment.ModifiedBy = "system";
                    existingAssignment.ModifiedAt = DateTime.UtcNow;
                    _ = await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Reactivated license assignment for tenant {TenantId}", tenantId);
                }
                
                return true;
            }

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
                TenantId = tenantId // License assignment belongs to the tenant it's assigned to
            };

            _ = _dbContext.TenantLicenses.Add(tenantLicense);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License assigned to tenant: {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning license to tenant");
            return false;
        }
    }

    private async Task SyncSuperAdminLicenseFeaturesAsync(Guid licenseId, CancellationToken cancellationToken)
    {
        try
        {
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

            var existingFeatures = await _dbContext.LicenseFeatures
                .Where(lf => lf.LicenseId == licenseId)
                .ToListAsync(cancellationToken);

            var featuresAdded = 0;
            var featuresUpdated = 0;

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
                    }
                }
            }

            var expectedNames = expectedFeatures.Select(f => f.Name).ToHashSet();
            var obsoleteFeatures = existingFeatures.Where(f => !expectedNames.Contains(f.Name) && f.IsActive).ToList();

            foreach (var obsolete in obsoleteFeatures)
            {
                obsolete.IsActive = false;
                obsolete.ModifiedBy = "system";
                obsolete.ModifiedAt = DateTime.UtcNow;
            }

            if (featuresAdded > 0 || featuresUpdated > 0 || obsoleteFeatures.Count > 0)
            {
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "SuperAdmin license features synchronized: {Added} added, {Updated} updated, {Deactivated} deactivated",
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
}
