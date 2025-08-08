using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;

namespace EventForge.Server.Services.Licensing;

/// <summary>
/// Service to seed initial licensing data for testing and demonstration.
/// </summary>
public static class LicensingSeedData
{
    /// <summary>
    /// Seeds the database with sample licenses, features, and permissions.
    /// </summary>
    /// <param name="context">Database context</param>
    public static async Task SeedLicensingDataAsync(EventForgeDbContext context)
    {
        // Check if licenses already exist
        if (await context.Licenses.AnyAsync())
        {
            return; // Data already seeded
        }

        // Get some existing permissions for features
        var permissions = await context.Permissions.ToListAsync();
        if (!permissions.Any())
        {
            // Create some basic permissions if they don't exist
            var basicPermissions = new[]
            {
                new Permission { Name = "events.read", DisplayName = "Read Events", Description = "Permission to view events", Category = "Events", Action = "Read", TenantId = Guid.Empty },
                new Permission { Name = "events.create", DisplayName = "Create Events", Description = "Permission to create events", Category = "Events", Action = "Create", TenantId = Guid.Empty },
                new Permission { Name = "events.update", DisplayName = "Update Events", Description = "Permission to update events", Category = "Events", Action = "Update", TenantId = Guid.Empty },
                new Permission { Name = "events.delete", DisplayName = "Delete Events", Description = "Permission to delete events", Category = "Events", Action = "Delete", TenantId = Guid.Empty },
                new Permission { Name = "teams.read", DisplayName = "Read Teams", Description = "Permission to view teams", Category = "Teams", Action = "Read", TenantId = Guid.Empty },
                new Permission { Name = "teams.create", DisplayName = "Create Teams", Description = "Permission to create teams", Category = "Teams", Action = "Create", TenantId = Guid.Empty },
                new Permission { Name = "reports.read", DisplayName = "Read Reports", Description = "Permission to view reports", Category = "Reports", Action = "Read", TenantId = Guid.Empty },
                new Permission { Name = "reports.advanced", DisplayName = "Advanced Reports", Description = "Permission to access advanced reporting features", Category = "Reports", Action = "Advanced", TenantId = Guid.Empty },
                new Permission { Name = "products.read", DisplayName = "Read Products", Description = "Permission to view products", Category = "Products", Action = "Read", TenantId = Guid.Empty },
                new Permission { Name = "products.create", DisplayName = "Create Products", Description = "Permission to create products", Category = "Products", Action = "Create", TenantId = Guid.Empty },
                new Permission { Name = "integrations.api", DisplayName = "API Integration", Description = "Permission to use API integrations", Category = "Integrations", Action = "Access", TenantId = Guid.Empty },
                new Permission { Name = "notifications.manage", DisplayName = "Manage Notifications", Description = "Permission to manage notifications", Category = "Notifications", Action = "Manage", TenantId = Guid.Empty }
            };

            context.Permissions.AddRange(basicPermissions);
            await context.SaveChangesAsync();
            permissions = basicPermissions.ToList();
        }

        // Create Basic License
        var basicLicense = new License
        {
            Name = "basic",
            DisplayName = "Basic License",
            Description = "Licenza base con funzionalità essenziali per piccole organizzazioni",
            MaxUsers = 5,
            MaxApiCallsPerMonth = 1000,
            TierLevel = 1,
            TenantId = Guid.Empty
        };

        // Create Standard License
        var standardLicense = new License
        {
            Name = "standard",
            DisplayName = "Standard License",
            Description = "Licenza standard con funzionalità avanzate per organizzazioni medie",
            MaxUsers = 25,
            MaxApiCallsPerMonth = 10000,
            TierLevel = 2,
            TenantId = Guid.Empty
        };

        // Create Premium License
        var premiumLicense = new License
        {
            Name = "premium",
            DisplayName = "Premium License",
            Description = "Licenza premium con tutte le funzionalità per grandi organizzazioni",
            MaxUsers = 100,
            MaxApiCallsPerMonth = 50000,
            TierLevel = 3,
            TenantId = Guid.Empty
        };

        // Create Enterprise License
        var enterpriseLicense = new License
        {
            Name = "enterprise",
            DisplayName = "Enterprise License",
            Description = "Licenza enterprise con funzionalità illimitate e supporto dedicato",
            MaxUsers = 1000,
            MaxApiCallsPerMonth = 500000,
            TierLevel = 4,
            TenantId = Guid.Empty
        };

        context.Licenses.AddRange(basicLicense, standardLicense, premiumLicense, enterpriseLicense);
        await context.SaveChangesAsync();

        // Create License Features
        await CreateLicenseFeatures(context, basicLicense, permissions, "basic");
        await CreateLicenseFeatures(context, standardLicense, permissions, "standard");
        await CreateLicenseFeatures(context, premiumLicense, permissions, "premium");
        await CreateLicenseFeatures(context, enterpriseLicense, permissions, "enterprise");

        await context.SaveChangesAsync();
    }

    private static async Task CreateLicenseFeatures(EventForgeDbContext context, License license, List<Permission> permissions, string tier)
    {
        var features = new List<LicenseFeature>();

        // Basic features available in all licenses
        features.Add(new LicenseFeature
        {
            Name = "BasicEventManagement",
            DisplayName = "Gestione Eventi Base",
            Description = "Funzionalità base per la gestione degli eventi",
            Category = "Events",
            LicenseId = license.Id,
            TenantId = Guid.Empty
        });

        features.Add(new LicenseFeature
        {
            Name = "BasicTeamManagement",
            DisplayName = "Gestione Team Base",
            Description = "Funzionalità base per la gestione dei team",
            Category = "Teams",
            LicenseId = license.Id,
            TenantId = Guid.Empty
        });

        // Standard features (available from standard tier up)
        if (tier != "basic")
        {
            features.Add(new LicenseFeature
            {
                Name = "BasicReporting",
                DisplayName = "Report Base",
                Description = "Funzionalità di reporting standard",
                Category = "Reports",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });

            features.Add(new LicenseFeature
            {
                Name = "ProductManagement",
                DisplayName = "Gestione Prodotti",
                Description = "Funzionalità per la gestione dei prodotti",
                Category = "Products",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });

            features.Add(new LicenseFeature
            {
                Name = "NotificationManagement",
                DisplayName = "Gestione Notifiche",
                Description = "Funzionalità avanzate per le notifiche",
                Category = "Notifications",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });
        }

        // Premium features (available from premium tier up)
        if (tier == "premium" || tier == "enterprise")
        {
            features.Add(new LicenseFeature
            {
                Name = "AdvancedReporting",
                DisplayName = "Report Avanzati",
                Description = "Funzionalità di reporting avanzate e analisi",
                Category = "Reports",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });

            features.Add(new LicenseFeature
            {
                Name = "ApiIntegrations",
                DisplayName = "Integrazioni API",
                Description = "Accesso completo alle API per integrazioni esterne",
                Category = "Integrations",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });
        }

        // Enterprise features (available only in enterprise)
        if (tier == "enterprise")
        {
            features.Add(new LicenseFeature
            {
                Name = "CustomIntegrations",
                DisplayName = "Integrazioni Custom",
                Description = "Integrazioni personalizzate e webhook",
                Category = "Integrations",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });

            features.Add(new LicenseFeature
            {
                Name = "AdvancedSecurity",
                DisplayName = "Sicurezza Avanzata",
                Description = "Funzionalità di sicurezza avanzate",
                Category = "Security",
                LicenseId = license.Id,
                TenantId = Guid.Empty
            });
        }

        context.LicenseFeatures.AddRange(features);
        await context.SaveChangesAsync();

        // Create License Feature Permissions mappings
        foreach (var feature in features)
        {
            var featurePermissions = GetPermissionsForFeature(feature.Name, permissions);
            
            foreach (var permission in featurePermissions)
            {
                context.LicenseFeaturePermissions.Add(new LicenseFeaturePermission
                {
                    LicenseFeatureId = feature.Id,
                    PermissionId = permission.Id,
                    TenantId = Guid.Empty
                });
            }
        }
    }

    private static List<Permission> GetPermissionsForFeature(string featureName, List<Permission> permissions)
    {
        return featureName switch
        {
            "BasicEventManagement" => permissions.Where(p => p.Category == "Events" && (p.Action == "Read" || p.Action == "Create")).ToList(),
            "BasicTeamManagement" => permissions.Where(p => p.Category == "Teams" && (p.Action == "Read" || p.Action == "Create")).ToList(),
            "BasicReporting" => permissions.Where(p => p.Category == "Reports" && p.Action == "Read").ToList(),
            "ProductManagement" => permissions.Where(p => p.Category == "Products").ToList(),
            "NotificationManagement" => permissions.Where(p => p.Category == "Notifications").ToList(),
            "AdvancedReporting" => permissions.Where(p => p.Category == "Reports").ToList(),
            "ApiIntegrations" => permissions.Where(p => p.Category == "Integrations").ToList(),
            "CustomIntegrations" => permissions.Where(p => p.Category == "Integrations").ToList(),
            "AdvancedSecurity" => permissions.Where(p => p.Category == "Security").ToList(),
            _ => new List<Permission>()
        };
    }

    /// <summary>
    /// Assigns a basic license to a test tenant for demonstration.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="tenantId">Tenant ID to assign license to</param>
    public static async Task AssignBasicLicenseToTenantAsync(EventForgeDbContext context, Guid tenantId)
    {
        // Check if tenant already has a license
        var existingLicense = await context.TenantLicenses
            .FirstOrDefaultAsync(tl => tl.TargetTenantId == tenantId && tl.IsLicenseActive);

        if (existingLicense != null)
        {
            return; // Tenant already has a license
        }

        // Get the basic license
        var basicLicense = await context.Licenses
            .FirstOrDefaultAsync(l => l.Name == "basic");

        if (basicLicense == null)
        {
            return; // Basic license not found
        }

        // Verify tenant exists
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            return; // Tenant not found
        }

        // Assign the license
        var tenantLicense = new TenantLicense
        {
            TargetTenantId = tenantId,
            LicenseId = basicLicense.Id,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1), // 1 year license
            IsLicenseActive = true,
            ApiCallsThisMonth = 0,
            ApiCallsResetAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            TenantId = Guid.Empty // System-level entity
        };

        context.TenantLicenses.Add(tenantLicense);
        await context.SaveChangesAsync();
    }
}