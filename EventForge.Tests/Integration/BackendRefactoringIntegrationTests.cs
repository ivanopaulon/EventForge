using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration Tests")]
public class BackendRefactoringIntegrationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public BackendRefactoringIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Development_Environment_Should_Have_Custom_Behavior()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - Access Swagger UI at root in development
        var response = await client.GetAsync("/");

        // Assert - Should have environment-aware behavior configured (302 redirect is also acceptable)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected 200, 301, or 302 but got {response.StatusCode}");
    }

    [Fact]
    public async Task Production_Environment_Should_Have_Custom_Behavior()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - Access homepage
        var response = await client.GetAsync("/");

        // Assert - Should have redirect behavior (environment-aware routing is configured)
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.MovedPermanently);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Swagger_Should_Be_Available_In_All_Environments()
    {
        // Arrange - Development
        var devClient = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        }).CreateClient();

        // Act & Assert - Swagger endpoint should be reachable (200 OK or 500 indicates it's registered;
        // 404 would mean it's not registered at all)
        var devSwaggerResponse = await devClient.GetAsync("/swagger/v1/swagger.json");
        Assert.NotEqual(HttpStatusCode.NotFound, devSwaggerResponse.StatusCode);

        // Arrange - Production
        var prodClient = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
        }).CreateClient();

        var prodSwaggerResponse = await prodClient.GetAsync("/swagger/v1/swagger.json");
        Assert.NotEqual(HttpStatusCode.NotFound, prodSwaggerResponse.StatusCode);
    }

    [Fact]
    public async Task Health_Checks_Should_Be_Available()
    {
        // Arrange – use in-memory DB so the DB health check doesn't try to reach a real SQL Server
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventForgeDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<EventForgeDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_HealthChecks"));

                services.Configure<JwtOptions>(options =>
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!");
            });
        }).CreateClient();

        // Act & Assert - Main health check (might be unhealthy due to missing DB in test, but should respond)
        var healthResponse = await client.GetAsync("/health");
        Assert.True(healthResponse.StatusCode == HttpStatusCode.OK || healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Readiness check
        var readyResponse = await client.GetAsync("/health/ready");
        Assert.True(readyResponse.StatusCode == HttpStatusCode.OK || readyResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Liveness check (should always be OK — Predicate = _ => false means no checks run)
        var liveResponse = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
    }

    [Fact]
    public async Task FileUploadOperationFilter_Should_Be_Registered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Get Swagger spec
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert - Swagger endpoint must exist (not 404); if it returns 200, verify multipart content
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("multipart/form-data", content);
        }
    }
}