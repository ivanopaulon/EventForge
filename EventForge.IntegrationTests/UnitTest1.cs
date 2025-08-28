using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace EventForge.IntegrationTests;

public class HealthCheckIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApiV1Health_EndpointExists()
    {
        // Arrange
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
                    options.UseInMemoryDatabase("TestDb");
                });

                // Override JWT secret for testing
                services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        // The endpoint should exist (not return 404), even if unhealthy
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BasicHealthCheck_ShouldWork()
    {
        // This is a very basic test to ensure the application starts
        var client = _factory.CreateClient();

        // Act - just test that we can make a request
        var response = await client.GetAsync("/");

        // Assert - should not be a server error
        Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);
    }
}