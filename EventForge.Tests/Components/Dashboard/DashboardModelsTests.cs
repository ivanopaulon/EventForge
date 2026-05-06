using Prym.DTOs.VatRates;
using Prym.Web.Shared.Components.Dashboard;

namespace EventForge.Tests.Components.Dashboard;

/// <summary>
/// Tests for dashboard models and functionality.
/// </summary>
[Trait("Category", "Unit")]
public class DashboardModelsTests
{
    [Fact]
    public void DashboardFilters_SetAndGetValue_ShouldWorkCorrectly()
    {
        // Arrange
        var filters = new DashboardFilters();

        // Act
        filters.SetValue("status", "Active");
        filters.SetValue("count", 42);

        // Assert
        Assert.Equal("Active", filters.GetValue<string>("status"));
        Assert.Equal(42, filters.GetValue<int>("count"));
    }

    [Fact]
    public void DashboardFilters_GetValue_MissingKey_ShouldReturnDefault()
    {
        // Arrange
        var filters = new DashboardFilters();

        // Act & Assert
        Assert.Null(filters.GetValue<string>("nonexistent"));
        Assert.Equal(0, filters.GetValue<int>("nonexistent"));
    }

    [Fact]
    public void DashboardFilters_SetValue_OverwritesPreviousValue()
    {
        // Arrange
        var filters = new DashboardFilters();
        filters.SetValue("key", "first");

        // Act
        filters.SetValue("key", "second");

        // Assert
        Assert.Equal("second", filters.GetValue<string>("key"));
    }

    [Fact]
    public void DashboardFilters_SetNullValue_ShouldReturnDefault()
    {
        // Arrange
        var filters = new DashboardFilters();

        // Act
        filters.SetValue("nullKey", null);

        // Assert
        Assert.Null(filters.GetValue<string>("nullKey"));
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
    public void DashboardFilterDefinition_ShouldStorePropertiesCorrectly()
    {
        // Arrange & Act
        var filterDef = new DashboardFilterDefinition
        {
            Id = "statusFilter",
            Label = "Stato",
            Type = FilterType.Select,
            DefaultValue = "Active",
            Options = new List<FilterOption>
            {
                new() { Value = "Active",    Label = "Attivo" },
                new() { Value = "Suspended", Label = "Sospeso" }
            }
        };

        // Assert
        Assert.Equal("statusFilter", filterDef.Id);
        Assert.Equal("Stato", filterDef.Label);
        Assert.Equal(FilterType.Select, filterDef.Type);
        Assert.Equal("Active", filterDef.DefaultValue);
        Assert.NotNull(filterDef.Options);
        Assert.Equal(2, filterDef.Options.Count);
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
}
