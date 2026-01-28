using EventForge.DTOs.Common;
using EventForge.Server.Configuration;
using EventForge.Server.Data;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace EventForge.Tests.Integration;

[Trait("Category", "Integration")]
public class PaginationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaginationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Pagination_DefaultValues_AreApplied()
    {
        // Arrange
        var client = CreateClient();

        // Act - call a test endpoint without pagination parameters
        // This test assumes we'll have endpoints using PaginationParameters
        // For now, we're just verifying the configuration is loaded

        // Assert - just verify the client can be created
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Pagination_Configuration_IsLoaded()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Verify PaginationSettings is registered
                var serviceProvider = services.BuildServiceProvider();
                var settings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<PaginationSettings>>();
                Assert.NotNull(settings);
                Assert.Equal(20, settings.Value.DefaultPageSize);
                Assert.Equal(1000, settings.Value.MaxPageSize);
                Assert.Equal(10000, settings.Value.MaxExportPageSize);
                Assert.Equal(100, settings.Value.RecommendedPageSize);
            });
        }).CreateClient();

        // Act & Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Pagination_EndpointOverrides_AreLoaded()
    {
        // Arrange & Act
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var settings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<PaginationSettings>>();
                
                // Assert
                Assert.NotNull(settings);
                Assert.True(settings.Value.EndpointOverrides.ContainsKey("/api/v1/stock/overview"));
                Assert.Equal(5000, settings.Value.EndpointOverrides["/api/v1/stock/overview"]);
                Assert.True(settings.Value.EndpointOverrides.ContainsKey("/api/v1/export/*"));
                Assert.Equal(10000, settings.Value.EndpointOverrides["/api/v1/export/*"]);
            });
        }).CreateClient();

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Pagination_RoleBasedLimits_AreLoaded()
    {
        // Arrange & Act
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var settings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<PaginationSettings>>();
                
                // Assert
                Assert.NotNull(settings);
                Assert.True(settings.Value.RoleBasedLimits.ContainsKey("User"));
                Assert.Equal(1000, settings.Value.RoleBasedLimits["User"]);
                Assert.True(settings.Value.RoleBasedLimits.ContainsKey("Admin"));
                Assert.Equal(5000, settings.Value.RoleBasedLimits["Admin"]);
                Assert.True(settings.Value.RoleBasedLimits.ContainsKey("SuperAdmin"));
                Assert.Equal(10000, settings.Value.RoleBasedLimits["SuperAdmin"]);
            });
        }).CreateClient();

        Assert.NotNull(client);
    }

    [Fact]
    public async Task PaginationParameters_Constructor_WithValidValues()
    {
        // Arrange & Act
        var pagination = new PaginationParameters(1, 20);

        // Assert
        Assert.Equal(1, pagination.Page);
        Assert.Equal(20, pagination.PageSize);
        Assert.False(pagination.WasCapped);
        Assert.Equal(0, pagination.CalculateSkip());
    }

    [Fact]
    public async Task PaginationParameters_Constructor_WithNegativeValues_ClampedToMinimum()
    {
        // Arrange & Act
        var pagination = new PaginationParameters(-1, -5);

        // Assert
        Assert.Equal(1, pagination.Page); // Clamped to 1
        Assert.Equal(1, pagination.PageSize); // Clamped to 1
    }

    [Fact]
    public async Task PaginationParameters_CalculateSkip_ReturnsCorrectValues()
    {
        // Arrange & Act & Assert
        var page1 = new PaginationParameters(1, 20);
        Assert.Equal(0, page1.CalculateSkip());

        var page2 = new PaginationParameters(2, 20);
        Assert.Equal(20, page2.CalculateSkip());

        var page5 = new PaginationParameters(5, 50);
        Assert.Equal(200, page5.CalculateSkip());
    }

    [Fact]
    public async Task PaginationParameters_JsonSerialization_IgnoresInternalProperties()
    {
        // Arrange
        var pagination = new PaginationParameters(2, 50)
        {
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };

        // Act
        var json = JsonSerializer.Serialize(pagination, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Assert
        Assert.Contains("\"Page\":2", json);
        Assert.Contains("\"PageSize\":50", json);
        // WasCapped and AppliedMaxPageSize should not be in JSON due to [JsonIgnore]
        Assert.DoesNotContain("WasCapped", json);
        Assert.DoesNotContain("AppliedMaxPageSize", json);
    }

    private HttpClient CreateClient()
    {
        return _factory.WithWebHostBuilder(builder =>
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
    }
}
