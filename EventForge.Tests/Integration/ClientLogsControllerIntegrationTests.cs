using EventForge.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
public class ClientLogsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientLogsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LogClientEntry_WithoutAuthentication_ShouldSucceed()
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
                    options.UseInMemoryDatabase("TestDb_ClientLogs");
                });

                // Override JWT secret for testing
                services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        var clientLog = new ClientLogDto
        {
            Level = "Information",
            Message = "Test log message from unauthenticated client",
            Page = "/test-page",
            Category = "IntegrationTest",
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ClientLogs", clientLog);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task LogClientBatch_WithoutAuthentication_ShouldSucceed()
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
                    options.UseInMemoryDatabase("TestDb_ClientLogsBatch");
                });

                // Override JWT secret for testing
                services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        var batchRequest = new ClientLogBatchDto
        {
            Logs = new List<ClientLogDto>
            {
                new ClientLogDto
                {
                    Level = "Information",
                    Message = "Test log 1",
                    Category = "IntegrationTest"
                },
                new ClientLogDto
                {
                    Level = "Error",
                    Message = "Test log 2",
                    Category = "IntegrationTest"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ClientLogs/batch", batchRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task LogClientEntry_WithInvalidData_ShouldReturnBadRequest()
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
                    options.UseInMemoryDatabase("TestDb_ClientLogsInvalid");
                });

                // Override JWT secret for testing
                services.Configure<JwtOptions>(options =>
                {
                    options.SecretKey = "TestSecretKeyThatIsAtLeast32CharsLong!!";
                });
            });
        }).CreateClient();

        var clientLog = new ClientLogDto
        {
            Level = "", // Invalid: empty level
            Message = "", // Invalid: empty message
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ClientLogs", clientLog);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
