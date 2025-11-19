using EventForge.Client.Shared.Components.Dashboard;
using EventForge.DTOs.VatRates;

namespace EventForge.Tests.Components.Dashboard;

/// <summary>
/// Tests for dashboard models and functionality.
/// </summary>
[Trait("Category", "Unit")]
public class DashboardModelsTests
{
    // Tests for DashboardFilters removed - type not yet implemented
    // TODO: Re-enable when DashboardFilters is implemented

    [Fact]
    public void DashboardMetric_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var metric = new DashboardMetric<VatRateDto>
        {
            Title = "Test Metric",
            Type = MetricType.Count
        };

        // Assert
        Assert.Equal("Test Metric", metric.Title);
        Assert.Equal(MetricType.Count, metric.Type);
        Assert.Equal(0, metric.TopN);
        Assert.False(metric.ShowChart);
    }

    [Fact]
    public void MetricResult_ShouldStoreCalculatedValues()
    {
        // Arrange & Act
        var result = new MetricResult
        {
            Title = "Total Count",
            Value = 42,
            FormattedValue = "42",
            Icon = "some-icon",
            Color = "primary"
        };

        // Assert
        Assert.Equal("Total Count", result.Title);
        Assert.Equal(42, result.Value);
        Assert.Equal("42", result.FormattedValue);
        Assert.Equal("some-icon", result.Icon);
        Assert.Equal("primary", result.Color);
    }

    // Test for DashboardFilterDefinition removed - type not yet implemented
    // TODO: Re-enable when DashboardFilterDefinition is implemented

    [Theory]
    [InlineData(MetricType.Count)]
    [InlineData(MetricType.Sum)]
    [InlineData(MetricType.Average)]
    [InlineData(MetricType.Min)]
    [InlineData(MetricType.Max)]
    public void MetricType_ShouldHaveAllExpectedValues(MetricType metricType)
    {
        // Assert - verify the enum value exists
        Assert.True(Enum.IsDefined(typeof(MetricType), metricType));
    }

    // Test for FilterType removed - type not yet implemented
    // TODO: Re-enable when FilterType is implemented

    [Fact]
    public void ChartDataPoint_ShouldStoreDataCorrectly()
    {
        // Arrange & Act
        var dataPoint = new ChartDataPoint
        {
            Label = "January",
            Value = 100.5
        };

        // Assert
        Assert.Equal("January", dataPoint.Label);
        Assert.Equal(100.5, dataPoint.Value);
    }

    [Fact]
    public void ServerMetricRequest_ShouldInitializeWithEmptyCollections()
    {
        // Arrange & Act
        var request = new ServerMetricRequest();

        // Assert
        Assert.NotNull(request.MetricIds);
        Assert.Empty(request.MetricIds);
        Assert.NotNull(request.Filters);
        Assert.Empty(request.Filters);
    }

    [Fact]
    public void ServerMetricResponse_ShouldInitializeWithEmptyMetrics()
    {
        // Arrange & Act
        var response = new ServerMetricResponse();

        // Assert
        Assert.NotNull(response.Metrics);
        Assert.Empty(response.Metrics);
    }

    // Tests for DashboardFilters type conversion removed - type not yet implemented
    // TODO: Re-enable when DashboardFilters is implemented
}
