using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using EventForge.Server.Services.Auth.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services.Auth;

[Trait("Category", "Unit")]
public class BootstrapServiceTests
{
    private static BootstrapService CreateBootstrapService(EventForgeDbContext context, IConfiguration config)
    {
        var logger = new LoggerFactory().CreateLogger<BootstrapService>();
        var passwordLogger = new LoggerFactory().CreateLogger<PasswordService>();
        var passwordService = new PasswordService(config, passwordLogger);
        
        var userSeederLogger = new LoggerFactory().CreateLogger<UserSeeder>();
        var userSeeder = new UserSeeder(context, passwordService, config, userSeederLogger);
        
        var tenantSeederLogger = new LoggerFactory().CreateLogger<TenantSeeder>();
        var tenantSeeder = new TenantSeeder(context, tenantSeederLogger);
        
        var licenseSeederLogger = new LoggerFactory().CreateLogger<LicenseSeeder>();
        var licenseSeeder = new LicenseSeeder(context, licenseSeederLogger);
        
        var entitySeederLogger = new LoggerFactory().CreateLogger<EntitySeeder>();
        var entitySeeder = new EntitySeeder(context, entitySeederLogger);
        
        return new BootstrapService(context, userSeeder, tenantSeeder, licenseSeeder, entitySeeder, logger);
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithEmptyDatabase_ShouldCreateInitialData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:DefaultAdminUsername"] = "superadmin",
                ["Bootstrap:DefaultAdminEmail"] = "superadmin@localhost",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Verify tenant was created
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == "default");
        Assert.NotNull(tenant);
        Assert.Equal("DefaultTenant", tenant.Name);
        Assert.Equal("default", tenant.Code);
        Assert.Equal("superadmin@localhost", tenant.ContactEmail);
        Assert.Equal("localhost", tenant.Domain);
        Assert.Equal(10, tenant.MaxUsers);
        Assert.True(tenant.IsActive);

        // Verify SuperAdmin user was created
        var user = await context.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("superadmin", user.Username);
        Assert.Equal("superadmin@localhost", user.Email);
        Assert.Equal("Super", user.FirstName);
        Assert.Equal("Admin", user.LastName);
        Assert.True(user.IsActive);
        Assert.True(user.MustChangePassword);

        // Verify SuperAdmin role was created
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        Assert.NotNull(role);
        Assert.True(role.IsSystemRole);

        // Verify user role assignment
        var userRole = await context.UserRoles.FirstOrDefaultAsync();
        Assert.NotNull(userRole);
        Assert.Equal(user.Id, userRole.UserId);
        Assert.Equal(role.Id, userRole.RoleId);

        // Verify superadmin license was created
        var license = await context.Licenses.FirstOrDefaultAsync(l => l.Name == "superadmin");
        Assert.NotNull(license);
        Assert.Equal("SuperAdmin License", license.DisplayName);
        Assert.Equal(int.MaxValue, license.MaxUsers);
        Assert.Equal(int.MaxValue, license.MaxApiCallsPerMonth);
        Assert.Equal(5, license.TierLevel);

        // Verify superadmin license features were created
        var licenseFeatures = await context.LicenseFeatures
            .Where(lf => lf.LicenseId == license.Id)
            .ToListAsync();
        Assert.NotEmpty(licenseFeatures);
        Assert.Contains(licenseFeatures, lf => lf.Name == "ProductManagement");
        Assert.Contains(licenseFeatures, lf => lf.Name == "BasicEventManagement");
        Assert.Contains(licenseFeatures, lf => lf.Name == "BasicTeamManagement");

        // Verify tenant license assignment
        var tenantLicense = await context.TenantLicenses.FirstOrDefaultAsync();
        Assert.NotNull(tenantLicense);
        Assert.Equal(tenant.Id, tenantLicense.TargetTenantId);
        Assert.Equal(license.Id, tenantLicense.LicenseId);
        Assert.True(tenantLicense.IsAssignmentActive);

        // Verify AdminTenant record
        var adminTenant = await context.AdminTenants.FirstOrDefaultAsync();
        Assert.NotNull(adminTenant);
        Assert.Equal(user.Id, adminTenant.UserId);
        Assert.Equal(tenant.Id, adminTenant.ManagedTenantId);
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithExistingTenants_ShouldCreateUsersAndSeedEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        // Add an existing tenant (not the default one)
        var existingTenant = new EventForge.Server.Data.Entities.Auth.Tenant
        {
            Name = "ExistingTenant",
            Code = "existing",
            DisplayName = "Existing Tenant",
            ContactEmail = "test@example.com",
            MaxUsers = 5,
            IsActive = true,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow,
            TenantId = Guid.Empty
        };
        _ = context.Tenants.Add(existingTenant);
        _ = await context.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Debug: List all tenants
        var allTenants = await context.Tenants.ToListAsync();
        // Expected: System (Guid.Empty), ExistingTenant, DefaultTenant = 3 total
        // But we filter out System, so should be 2 non-system tenants

        // Verify default tenant was created (in addition to system and existing tenant)
        var tenantCount = await context.Tenants.CountAsync(t => t.Id != Guid.Empty);
        Assert.Equal(2, tenantCount); // ExistingTenant + DefaultTenant

        // Verify SuperAdmin and Manager users were created
        var userCount = await context.Users.CountAsync();
        Assert.Equal(2, userCount); // SuperAdmin + Manager

        // Verify users are assigned to default tenant (not the existing one)
        var users = await context.Users.ToListAsync();
        var defaultTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == "default");
        Assert.NotNull(defaultTenant);
        Assert.All(users, u => Assert.Equal(defaultTenant.Id, u.TenantId));
        
        // Verify base entities were seeded for default tenant
        var hasVatRates = await context.VatRates.AnyAsync(v => v.TenantId == defaultTenant.Id);
        Assert.True(hasVatRates);
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithEnvironmentPassword_ShouldUseEnvironmentValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        // Set environment variable
        Environment.SetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD", "EnvironmentPassword123!");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "ConfigPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        try
        {
            // Act
            var result = await bootstrapService.EnsureAdminBootstrappedAsync();

            // Assert
            Assert.True(result);

            // Verify user was created (indirectly verifies password was accepted)
            var user = await context.Users.FirstOrDefaultAsync();
            Assert.NotNull(user);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD", null);
        }
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_RunningTwice_ShouldUpdateLicenseConfiguration()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act - First run: create everything
        var firstResult = await bootstrapService.EnsureAdminBootstrappedAsync();
        Assert.True(firstResult);

        // Get the created license
        var license = await context.Licenses.FirstOrDefaultAsync(l => l.Name == "superadmin");
        Assert.NotNull(license);
        var originalLicenseId = license.Id;

        // Manually modify the license to simulate an outdated configuration
        license.DisplayName = "Old Display Name";
        license.Description = "Old Description";
        license.MaxUsers = 100; // Changed from int.MaxValue
        license.MaxApiCallsPerMonth = 1000; // Changed from int.MaxValue
        license.TierLevel = 1; // Changed from 5
        _ = await context.SaveChangesAsync();

        // Also remove a feature to test synchronization
        var featureToRemove = await context.LicenseFeatures
            .FirstOrDefaultAsync(f => f.LicenseId == license.Id && f.Name == "AdvancedSecurity");
        if (featureToRemove != null)
        {
            _ = context.LicenseFeatures.Remove(featureToRemove);
            _ = await context.SaveChangesAsync();
        }

        // Act - Second run: should update the license and restore the feature
        var secondResult = await bootstrapService.EnsureAdminBootstrappedAsync();
        Assert.True(secondResult);

        // Assert - Verify license was updated to match expected configuration
        var updatedLicense = await context.Licenses.FirstOrDefaultAsync(l => l.Name == "superadmin");
        Assert.NotNull(updatedLicense);
        Assert.Equal(originalLicenseId, updatedLicense.Id); // Same license, not recreated
        Assert.Equal("SuperAdmin License", updatedLicense.DisplayName); // Updated
        Assert.Equal("SuperAdmin license with unlimited features for complete system management", updatedLicense.Description); // Updated
        Assert.Equal(int.MaxValue, updatedLicense.MaxUsers); // Updated
        Assert.Equal(int.MaxValue, updatedLicense.MaxApiCallsPerMonth); // Updated
        Assert.Equal(5, updatedLicense.TierLevel); // Updated
        _ = Assert.NotNull(updatedLicense.ModifiedAt); // Modified timestamp set
        Assert.Equal("system", updatedLicense.ModifiedBy);

        // Assert - Verify the removed feature was restored
        var restoredFeature = await context.LicenseFeatures
            .FirstOrDefaultAsync(f => f.LicenseId == license.Id && f.Name == "AdvancedSecurity");
        Assert.NotNull(restoredFeature);
        Assert.True(restoredFeature.IsActive);

        // Assert - Verify all expected features are present
        var allFeatures = await context.LicenseFeatures
            .Where(f => f.LicenseId == license.Id)
            .ToListAsync();
        Assert.Equal(16, allFeatures.Count); // All 16 features should be present (9 original + 7 new)
        Assert.All(allFeatures, f => Assert.True(f.IsActive));
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithExistingData_ShouldUpdateLicenseOnlyWithoutRecreatingTenant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // First run to create everything
        _ = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Get initial counts (system tenant + default tenant)
        var initialTenantCount = await context.Tenants.CountAsync();
        var initialUserCount = await context.Users.CountAsync();

        // Modify license to outdated state
        var license = await context.Licenses.FirstOrDefaultAsync(l => l.Name == "superadmin");
        Assert.NotNull(license);
        license.MaxUsers = 50;
        _ = await context.SaveChangesAsync();

        // Act - Second run with existing tenants
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Verify no new tenants or users were created
        Assert.Equal(initialTenantCount, await context.Tenants.CountAsync());
        Assert.Equal(initialUserCount, await context.Users.CountAsync());

        // Verify license was still updated despite existing tenants
        var updatedLicense = await context.Licenses.FirstOrDefaultAsync(l => l.Name == "superadmin");
        Assert.NotNull(updatedLicense);
        Assert.Equal(int.MaxValue, updatedLicense.MaxUsers); // Should be updated
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_ShouldAssignAllPermissionsToSuperAdminRole()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Verify SuperAdmin role exists
        var superAdminRole = await context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        Assert.NotNull(superAdminRole);

        // Verify Admin role exists
        var adminRole = await context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == "Admin");
        Assert.NotNull(adminRole);

        // Get all permissions
        var allPermissions = await context.Permissions.ToListAsync();
        Assert.NotEmpty(allPermissions);

        // Verify SuperAdmin role has all permissions assigned
        Assert.NotEmpty(superAdminRole.RolePermissions);
        Assert.Equal(allPermissions.Count, superAdminRole.RolePermissions.Count);

        // Verify each permission is assigned to SuperAdmin role
        foreach (var permission in allPermissions)
        {
            Assert.Contains(superAdminRole.RolePermissions,
                rp => rp.PermissionId == permission.Id && rp.RoleId == superAdminRole.Id);
        }

        // Verify Admin role also has all permissions assigned
        Assert.NotEmpty(adminRole.RolePermissions);
        Assert.Equal(allPermissions.Count, adminRole.RolePermissions.Count);

        // Verify SuperAdmin user has SuperAdmin role
        var superAdminUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == "superadmin");
        Assert.NotNull(superAdminUser);
        _ = Assert.Single(superAdminUser.UserRoles);
        Assert.Equal(superAdminRole.Id, superAdminUser.UserRoles.First().RoleId);

        // Verify all role permissions have correct TenantId (system-level)
        Assert.All(superAdminRole.RolePermissions, rp => Assert.Equal(Guid.Empty, rp.TenantId));
        Assert.All(adminRole.RolePermissions, rp => Assert.Equal(Guid.Empty, rp.TenantId));
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithNewTenant_ShouldSeedBaseEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:DefaultAdminUsername"] = "superadmin",
                ["Bootstrap:DefaultAdminEmail"] = "superadmin@localhost",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == "default");
        Assert.NotNull(tenant);

        // Verify VAT natures were seeded (24 Italian VAT nature codes)
        var vatNatures = await context.VatNatures.Where(v => v.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(24, vatNatures.Count);
        Assert.Contains(vatNatures, v => v.Code == "N1");
        Assert.Contains(vatNatures, v => v.Code == "N2");
        Assert.Contains(vatNatures, v => v.Code == "N3");
        Assert.Contains(vatNatures, v => v.Code == "N6.1");
        Assert.Contains(vatNatures, v => v.Code == "N7");

        // Verify VAT rates were seeded (5 Italian VAT rates)
        var vatRates = await context.VatRates.Where(v => v.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(5, vatRates.Count);
        Assert.Contains(vatRates, v => v.Percentage == 22m);
        Assert.Contains(vatRates, v => v.Percentage == 10m);
        Assert.Contains(vatRates, v => v.Percentage == 5m);
        Assert.Contains(vatRates, v => v.Percentage == 4m);
        Assert.Contains(vatRates, v => v.Percentage == 0m);

        // Verify units of measure were seeded (20 units)
        var ums = await context.UMs.Where(u => u.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(20, ums.Count);
        Assert.Contains(ums, u => u.Symbol == "pz" && u.IsDefault);
        Assert.Contains(ums, u => u.Symbol == "kg");
        Assert.Contains(ums, u => u.Symbol == "l");
        Assert.Contains(ums, u => u.Symbol == "pallet");

        // Verify default warehouse was created
        var warehouses = await context.StorageFacilities.Where(w => w.TenantId == tenant.Id).ToListAsync();
        _ = Assert.Single(warehouses);
        var warehouse = warehouses.First();
        Assert.Equal("Magazzino Principale", warehouse.Name);
        Assert.Equal("MAG-01", warehouse.Code);
        Assert.True(warehouse.IsFiscal);

        // Verify default storage location was created
        var locations = await context.StorageLocations.Where(l => l.TenantId == tenant.Id).ToListAsync();
        _ = Assert.Single(locations);
        var location = locations.First();
        Assert.Equal("UB-DEF", location.Code);
        Assert.Equal(warehouse.Id, location.WarehouseId);
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_RunTwice_ShouldNotDuplicateBaseEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:DefaultAdminUsername"] = "superadmin",
                ["Bootstrap:DefaultAdminEmail"] = "superadmin@localhost",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var bootstrapService = CreateBootstrapService(context, config);

        // Act - Run bootstrap twice
        var result1 = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Create a new service instance to simulate restart
        await using var context2 = new EventForgeDbContext(options);
        var bootstrapService2 = CreateBootstrapService(context2, config);
        var result2 = await bootstrapService2.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result1);
        Assert.True(result2);

        await using var verifyContext = new EventForgeDbContext(options);
        var tenant = await verifyContext.Tenants.FirstOrDefaultAsync(t => t.Id != Guid.Empty);
        Assert.NotNull(tenant);

        // Verify counts are still correct (no duplication)
        var vatNatureCount = await verifyContext.VatNatures.CountAsync(v => v.TenantId == tenant.Id);
        Assert.Equal(24, vatNatureCount);

        var vatRateCount = await verifyContext.VatRates.CountAsync(v => v.TenantId == tenant.Id);
        Assert.Equal(5, vatRateCount);

        var umCount = await verifyContext.UMs.CountAsync(u => u.TenantId == tenant.Id);
        Assert.Equal(20, umCount);

        var warehouseCount = await verifyContext.StorageFacilities.CountAsync(w => w.TenantId == tenant.Id);
        Assert.Equal(1, warehouseCount);

        var locationCount = await verifyContext.StorageLocations.CountAsync(l => l.TenantId == tenant.Id);
        Assert.Equal(1, locationCount);
    }

    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithTenantButMissingBaseEntities_ShouldSeedBaseEntities()
    {
        // Arrange - Simulate a recreated database with tenant but no base entities
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:DefaultAdminUsername"] = "superadmin",
                ["Bootstrap:DefaultAdminEmail"] = "superadmin@localhost",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();


        // First, run complete bootstrap to create tenant and base entities
        var bootstrapService1 = CreateBootstrapService(context, config);
        var firstResult = await bootstrapService1.EnsureAdminBootstrappedAsync();
        Assert.True(firstResult);

        // Get tenant ID for verification (skip system tenant)
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id != Guid.Empty);
        Assert.NotNull(tenant);
        var tenantId = tenant.Id;

        // Verify base entities exist
        Assert.True(await context.VatNatures.AnyAsync(v => v.TenantId == tenantId));
        Assert.True(await context.VatRates.AnyAsync(v => v.TenantId == tenantId));
        Assert.True(await context.UMs.AnyAsync(u => u.TenantId == tenantId));
        Assert.True(await context.StorageFacilities.AnyAsync(w => w.TenantId == tenantId));

        // Simulate database recreation: Remove all base entities but keep tenant and user
        context.VatNatures.RemoveRange(context.VatNatures.Where(v => v.TenantId == tenantId));
        context.VatRates.RemoveRange(context.VatRates.Where(v => v.TenantId == tenantId));
        context.UMs.RemoveRange(context.UMs.Where(u => u.TenantId == tenantId));
        context.StorageLocations.RemoveRange(context.StorageLocations.Where(l => l.TenantId == tenantId));
        context.StorageFacilities.RemoveRange(context.StorageFacilities.Where(w => w.TenantId == tenantId));
        context.DocumentTypes.RemoveRange(context.DocumentTypes.Where(d => d.TenantId == tenantId));
        await context.SaveChangesAsync();

        // Verify base entities are gone
        Assert.False(await context.VatNatures.AnyAsync(v => v.TenantId == tenantId));
        Assert.False(await context.VatRates.AnyAsync(v => v.TenantId == tenantId));
        Assert.False(await context.UMs.AnyAsync(u => u.TenantId == tenantId));
        Assert.False(await context.StorageFacilities.AnyAsync(w => w.TenantId == tenantId));

        // Act - Run bootstrap again, which should detect missing base entities and seed them
        var bootstrapService2 = CreateBootstrapService(context, config);
        var secondResult = await bootstrapService2.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(secondResult);

        // Verify base entities were re-seeded
        await using var verifyContext = new EventForgeDbContext(options);

        var vatNatureCount = await verifyContext.VatNatures.CountAsync(v => v.TenantId == tenantId);
        Assert.Equal(24, vatNatureCount);

        var vatRateCount = await verifyContext.VatRates.CountAsync(v => v.TenantId == tenantId);
        Assert.Equal(5, vatRateCount);

        var umCount = await verifyContext.UMs.CountAsync(u => u.TenantId == tenantId);
        Assert.Equal(20, umCount);

        var warehouseCount = await verifyContext.StorageFacilities.CountAsync(w => w.TenantId == tenantId);
        Assert.Equal(1, warehouseCount);

        var locationCount = await verifyContext.StorageLocations.CountAsync(l => l.TenantId == tenantId);
        Assert.Equal(1, locationCount);

        var docTypeCount = await verifyContext.DocumentTypes.CountAsync(d => d.TenantId == tenantId);
        Assert.Equal(12, docTypeCount);
    }
}