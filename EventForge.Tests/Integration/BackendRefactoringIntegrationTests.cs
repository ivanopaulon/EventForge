using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
public class BackendRefactoringIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
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

        // Assert - Should have environment-aware behavior configured
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.MovedPermanently);
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

        // Act & Assert - Development should have Swagger at root
        var devSwaggerResponse = await devClient.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, devSwaggerResponse.StatusCode);

        // Arrange - Production
        var prodClient = _factory.WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
        }).CreateClient();

        // Act & Assert - Production should have Swagger at /swagger
        var prodSwaggerResponse = await prodClient.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, prodSwaggerResponse.StatusCode);
    }

    [Fact]
    public async Task Health_Checks_Should_Be_Available()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert - Main health check (might be unhealthy due to missing DB in test, but should respond)
        var healthResponse = await client.GetAsync("/health");
        Assert.True(healthResponse.StatusCode == HttpStatusCode.OK || healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Readiness check (might be unhealthy due to missing DB in test, but should respond)
        var readyResponse = await client.GetAsync("/health/ready");
        Assert.True(readyResponse.StatusCode == HttpStatusCode.OK || readyResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Liveness check (should always be OK)
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
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should contain multipart/form-data for file upload endpoints
        Assert.Contains("multipart/form-data", content);
    }
}