using EventForge.Client.Shared.Components.Dashboard;
using EventForge.DTOs.VatRates;
using EventForge.DTOs.Common;

namespace EventForge.Tests.Components.Dashboard;

/// <summary>
/// Tests for dashboard models and functionality.
/// </summary>
[Trait("Category", "Unit")]
public class DashboardModelsTests
{
    [Fact]
    public void DashboardFilters_GetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var filters = new DashboardFilters();
        filters.SetValue("status", "active");
        filters.SetValue("count", 5);

        // Act
        var statusValue = filters.GetValue<string>("status");
        var countValue = filters.GetValue<int>("count");

        // Assert
        Assert.Equal("active", statusValue);
        Assert.Equal(5, countValue);
    }

    [Fact]
    public void DashboardFilters_GetValue_ShouldReturnDefaultForNonExistentKey()
    {
        // Arrange
        var filters = new DashboardFilters();

        // Act
        var value = filters.GetValue<string>("nonExistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void DashboardFilters_SetValue_ShouldUpdateExistingValue()
    {
        // Arrange
        var filters = new DashboardFilters();
        filters.SetValue("status", "active");

        // Act
        filters.SetValue("status", "suspended");
        var value = filters.GetValue<string>("status");

        // Assert
        Assert.Equal("suspended", value);
    }

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

    [Fact]
    public void DashboardFilterDefinition_WithSelectType_ShouldHaveOptions()
    {
        // Arrange & Act
        var filterDef = new DashboardFilterDefinition
        {
            Id = "status",
            Label = "Status",
            Type = FilterType.Select,
            Options = new List<FilterOption>
            {
                new() { Value = "active", Label = "Active" },
                new() { Value = "inactive", Label = "Inactive" }
            }
        };

        // Assert
        Assert.Equal("status", filterDef.Id);
        Assert.Equal(FilterType.Select, filterDef.Type);
        Assert.NotNull(filterDef.Options);
        Assert.Equal(2, filterDef.Options.Count);
        Assert.Equal("active", filterDef.Options[0].Value);
    }

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

    [Theory]
    [InlineData(FilterType.Text)]
    [InlineData(FilterType.Select)]
    [InlineData(FilterType.Date)]
    [InlineData(FilterType.DateRange)]
    [InlineData(FilterType.Checkbox)]
    [InlineData(FilterType.Number)]
    public void FilterType_ShouldHaveAllExpectedValues(FilterType filterType)
    {
        // Assert - verify the enum value exists
        Assert.True(Enum.IsDefined(typeof(FilterType), filterType));
    }

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

    [Fact]
    public void DashboardFilters_GetValue_ShouldHandleTypeConversion()
    {
        // Arrange
        var filters = new DashboardFilters();
        filters.SetValue("intValue", "42");

        // Act
        var value = filters.GetValue<int>("intValue");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void DashboardFilters_GetValue_ShouldReturnDefaultOnConversionError()
    {
        // Arrange
        var filters = new DashboardFilters();
        filters.SetValue("invalidInt", "not-a-number");

        // Act
        var value = filters.GetValue<int>("invalidInt");

        // Assert
        Assert.Equal(0, value); // default for int
    }
}
