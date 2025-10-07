using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
public class DocumentsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/documents")]
    [InlineData("/api/v1/documents/types")]
    public async Task DocumentsController_NewUnifiedEndpoints_ShouldExist(string endpoint)
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                // Use in-memory database for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventForgeDbContext>));
                if (descriptor != null)
                    _ = services.Remove(descriptor);

                _ = services.AddDbContext<EventForgeDbContext>(options =>
                {
                    _ = options.UseInMemoryDatabase("TestDb_DocumentsController");
                });

                // Override JWT secret for testing
                _ = services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        // The endpoints should exist (not return 404), even if they return 401 Unauthorized (no auth)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DocumentsController_MainDocumentEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                // Use in-memory database for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventForgeDbContext>));
                if (descriptor != null)
                    _ = services.Remove(descriptor);

                _ = services.AddDbContext<EventForgeDbContext>(options =>
                {
                    _ = options.UseInMemoryDatabase("TestDb_DocumentsMain");
                });

                // Override JWT secret for testing
                _ = services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/documents");

        // Assert
        // Should return 401 Unauthorized (requires authentication) rather than 404 Not Found
        // This confirms the route is correctly mapped
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DocumentsController_DocumentTypesEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                // Use in-memory database for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventForgeDbContext>));
                if (descriptor != null)
                    _ = services.Remove(descriptor);

                _ = services.AddDbContext<EventForgeDbContext>(options =>
                {
                    _ = options.UseInMemoryDatabase("TestDb_DocumentTypes");
                });

                // Override JWT secret for testing
                _ = services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/documents/types");

        // Assert
        // Should return 401 Unauthorized (requires authentication) rather than 404 Not Found
        // This confirms the route is correctly mapped
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}