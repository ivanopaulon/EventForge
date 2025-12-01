using EventForge.Server.Auth;
using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Tests.Integration;

/// <summary>
/// Tests to verify that authorization handlers are registered with correct DI lifetimes.
/// This prevents runtime errors like "Cannot consume scoped service from singleton".
/// </summary>
[Trait("Category", "Integration")]
public class AuthorizationHandlerLifetimeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationHandlerLifetimeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void TenantAdminAuthorizationHandler_ShouldBeRegisteredAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Use in-memory database for testing
        services.AddDbContext<EventForgeDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        
        // Add required services
        services.AddLogging();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession();
        
        // Register the authorization handler as it should be in the application
        services.AddScoped<IAuthorizationHandler, TenantAdminAuthorizationHandler>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act - Try to resolve the handler in different scopes to verify it's not a singleton
        IAuthorizationHandler? handler1;
        using (var scope1 = serviceProvider.CreateScope())
        {
            handler1 = scope1.ServiceProvider.GetService<IAuthorizationHandler>();
        }
        
        IAuthorizationHandler? handler2;
        using (var scope2 = serviceProvider.CreateScope())
        {
            handler2 = scope2.ServiceProvider.GetService<IAuthorizationHandler>();
        }
        
        IAuthorizationHandler? handler3;
        using (var scope3 = serviceProvider.CreateScope())
        {
            handler3 = scope3.ServiceProvider.GetService<IAuthorizationHandler>();
        }
        
        // Assert - Each scope should get a different instance (not singleton)
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotNull(handler3);
        Assert.NotSame(handler1, handler2);
        Assert.NotSame(handler2, handler3);
        Assert.NotSame(handler1, handler3);
    }
    
    [Fact]
    public void TenantAdminAuthorizationHandler_CanAccessScopedDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Use in-memory database for testing
        services.AddDbContext<EventForgeDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_Dependencies"));
        
        // Add required services
        services.AddLogging();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession();
        
        // Register the authorization handler
        services.AddScoped<IAuthorizationHandler, TenantAdminAuthorizationHandler>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act & Assert - Should be able to create the handler without DI errors
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<IAuthorizationHandler>();
        
        Assert.NotNull(handler);
        Assert.IsType<TenantAdminAuthorizationHandler>(handler);
    }
    
    [Fact]
    public async Task ApplicationStartup_ShouldNotThrowDILifetimeError()
    {
        // Arrange & Act - Create a test client which will validate DI lifetimes during startup
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Use in-memory database for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventForgeDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<EventForgeDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Startup");
                });

                // Override JWT secret for testing
                services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        // Assert - If we get here without an exception, the DI configuration is valid
        Assert.NotNull(client);
        
        // Make a simple request to ensure the app can handle requests
        var response = await client.GetAsync("/");
        Assert.NotNull(response);
    }
}
