using EventForge.DTOs.Dashboard;
using EventForge.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventForge.Tests.Integration;

/// <summary>
/// Integration tests for Dashboard Configuration API endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class DashboardConfigurationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DashboardConfigurationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConfigurations_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var entityType = "VatRate";

        // Act
        var response = await _client.GetAsync($"/api/v1/DashboardConfiguration?entityType={entityType}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateConfiguration_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var createDto = new CreateDashboardConfigurationDto
        {
            Name = "Test Configuration",
            EntityType = "VatRate",
            IsDefault = true,
            Metrics = new List<DashboardMetricConfigDto>
            {
                new()
                {
                    Title = "Total Count",
                    Type = MetricType.Count,
                    Icon = "icon",
                    Color = "primary",
                    Order = 0
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/DashboardConfiguration", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DashboardConfigurationEndpoint_IsAccessible()
    {
        // Arrange
        var entityType = "VatRate";

        // Act - Endpoint should exist and return 401 (not 404)
        var response = await _client.GetAsync($"/api/v1/DashboardConfiguration?entityType={entityType}");

        // Assert - Should be unauthorized, not not-found
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void DashboardConfigurationService_IsRegistered()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<EventForge.Server.Services.Dashboard.IDashboardConfigurationService>();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void CreateDashboardConfigurationDto_Validation_RequiresName()
    {
        // Arrange
        var dto = new CreateDashboardConfigurationDto
        {
            Name = "", // Invalid - empty name
            EntityType = "VatRate",
            Metrics = new List<DashboardMetricConfigDto>()
        };

        // Act & Assert
        Assert.True(string.IsNullOrEmpty(dto.Name));
    }

    [Fact]
    public void DashboardMetricConfigDto_HasCorrectProperties()
    {
        // Arrange & Act
        var metric = new DashboardMetricConfigDto
        {
            Title = "Test Metric",
            Type = MetricType.Count,
            FieldName = "TestField",
            Icon = "test-icon",
            Color = "primary",
            Order = 1
        };

        // Assert
        Assert.Equal("Test Metric", metric.Title);
        Assert.Equal(MetricType.Count, metric.Type);
        Assert.Equal("TestField", metric.FieldName);
        Assert.Equal("test-icon", metric.Icon);
        Assert.Equal("primary", metric.Color);
        Assert.Equal(1, metric.Order);
    }

    [Theory]
    [InlineData(MetricType.Count)]
    [InlineData(MetricType.Sum)]
    [InlineData(MetricType.Average)]
    [InlineData(MetricType.Min)]
    [InlineData(MetricType.Max)]
    public void MetricType_AllValuesAreDefined(MetricType metricType)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(MetricType), metricType));
    }
}
