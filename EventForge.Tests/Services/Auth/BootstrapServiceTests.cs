using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services.Auth;

[Trait("Category", "Unit")]
public class BootstrapServiceTests
{
    [Fact]
    public async Task EnsureAdminBootstrappedAsync_WithEmptyDatabase_ShouldCreateInitialData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:DefaultAdminUsername"] = "superadmin",
                ["Bootstrap:DefaultAdminEmail"] = "superadmin@localhost",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<BootstrapService>();
        var passwordLogger = new LoggerFactory().CreateLogger<PasswordService>();
        var passwordService = new PasswordService(config, passwordLogger);
        var bootstrapService = new BootstrapService(context, passwordService, config, logger);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Verify tenant was created
        var tenant = await context.Tenants.FirstOrDefaultAsync();
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
    public async Task EnsureAdminBootstrappedAsync_WithExistingTenants_ShouldSkipBootstrap()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new EventForgeDbContext(options);

        // Add an existing tenant
        context.Tenants.Add(new EventForge.Server.Data.Entities.Auth.Tenant
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
        });
        await context.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Bootstrap:SuperAdminPassword"] = "TestPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<BootstrapService>();
        var passwordLogger = new LoggerFactory().CreateLogger<PasswordService>();
        var passwordService = new PasswordService(config, passwordLogger);
        var bootstrapService = new BootstrapService(context, passwordService, config, logger);

        // Act
        var result = await bootstrapService.EnsureAdminBootstrappedAsync();

        // Assert
        Assert.True(result);

        // Verify no additional tenants were created
        var tenantCount = await context.Tenants.CountAsync();
        Assert.Equal(1, tenantCount);

        // Verify no users were created
        var userCount = await context.Users.CountAsync();
        Assert.Equal(0, userCount);
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
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Bootstrap:SuperAdminPassword"] = "ConfigPassword123!",
                ["Bootstrap:AutoCreateAdmin"] = "true"
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<BootstrapService>();
        var passwordLogger = new LoggerFactory().CreateLogger<PasswordService>();
        var passwordService = new PasswordService(config, passwordLogger);
        var bootstrapService = new BootstrapService(context, passwordService, config, logger);

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
}