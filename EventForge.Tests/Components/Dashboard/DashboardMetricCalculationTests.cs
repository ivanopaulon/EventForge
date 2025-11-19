using EventForge.Client.Shared.Components.Dashboard;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;

namespace EventForge.Tests.Components.Dashboard;

/// <summary>
/// Tests for dashboard metric calculation logic.
/// </summary>
[Trait("Category", "Unit")]
public class DashboardMetricCalculationTests
{
    private readonly List<VatRateDto> _testVatRates;

    public DashboardMetricCalculationTests()
    {
        _testVatRates = new List<VatRateDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "IVA 22%",
                Percentage = 22m,
                Status = VatRateStatus.Active,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "IVA 10%",
                Percentage = 10m,
                Status = VatRateStatus.Active,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "IVA 4%",
                Percentage = 4m,
                Status = VatRateStatus.Active,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "IVA 5% (Sospesa)",
                Percentage = 5m,
                Status = VatRateStatus.Suspended,
                IsActive = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "IVA 8% (Eliminata)",
                Percentage = 8m,
                Status = VatRateStatus.Deleted,
                IsActive = false
            }
        };
    }

    [Fact]
    public void CountMetric_ShouldCountAllItems()
    {
        // Arrange
        var items = _testVatRates;

        // Act
        var count = items.Count();

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public void CountMetric_WithFilter_ShouldCountFilteredItems()
    {
        // Arrange
        var items = _testVatRates;
        var filter = new Func<VatRateDto, bool>(v => v.Status == VatRateStatus.Active);

        // Act
        var count = items.Where(filter).Count();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void SumMetric_ShouldSumValues()
    {
        // Arrange
        var items = _testVatRates.Where(v => v.Status == VatRateStatus.Active);
        var selector = new Func<VatRateDto, decimal>(v => v.Percentage);

        // Act
        var sum = items.Sum(selector);

        // Assert
        Assert.Equal(36m, sum); // 22 + 10 + 4
    }

    [Fact]
    public void AverageMetric_ShouldCalculateAverage()
    {
        // Arrange
        var items = _testVatRates.Where(v => v.Status == VatRateStatus.Active);
        var selector = new Func<VatRateDto, decimal>(v => v.Percentage);

        // Act
        var average = items.Average(selector);

        // Assert
        Assert.Equal(12m, average); // (22 + 10 + 4) / 3
    }

    [Fact]
    public void MinMetric_ShouldFindMinimumValue()
    {
        // Arrange
        var items = _testVatRates.Where(v => v.Status == VatRateStatus.Active);
        var selector = new Func<VatRateDto, decimal>(v => v.Percentage);

        // Act
        var min = items.Min(selector);

        // Assert
        Assert.Equal(4m, min);
    }

    [Fact]
    public void MaxMetric_ShouldFindMaximumValue()
    {
        // Arrange
        var items = _testVatRates.Where(v => v.Status == VatRateStatus.Active);
        var selector = new Func<VatRateDto, decimal>(v => v.Percentage);

        // Act
        var max = items.Max(selector);

        // Assert
        Assert.Equal(22m, max);
    }

    [Fact]
    public void GroupedMetric_ShouldGroupByStatus()
    {
        // Arrange
        var items = _testVatRates;
        var groupBySelector = new Func<VatRateDto, object>(v => v.Status);

        // Act
        var groups = items.GroupBy(groupBySelector);

        // Assert
        Assert.Equal(3, groups.Count()); // Active, Suspended, Deleted
        Assert.Equal(3, groups.First(g => g.Key.Equals(VatRateStatus.Active)).Count());
        Assert.Equal(1, groups.First(g => g.Key.Equals(VatRateStatus.Suspended)).Count());
        Assert.Equal(1, groups.First(g => g.Key.Equals(VatRateStatus.Deleted)).Count());
    }

    [Fact]
    public void TopNMetric_ShouldLimitResults()
    {
        // Arrange
        var items = _testVatRates;
        var groupBySelector = new Func<VatRateDto, object>(v => v.Status);
        var topN = 2;

        // Act
        var topGroups = items.GroupBy(groupBySelector).Take(topN);

        // Assert
        Assert.Equal(2, topGroups.Count());
    }

    [Fact]
    public void MetricWithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var items = _testVatRates;
        var filter1 = new Func<VatRateDto, bool>(v => v.Status == VatRateStatus.Active);
        var filter2 = new Func<VatRateDto, bool>(v => v.Percentage >= 10m);

        // Act
        var filteredItems = items.Where(filter1).Where(filter2);
        var count = filteredItems.Count();

        // Assert
        Assert.Equal(2, count); // Only IVA 22% and IVA 10% match both filters
    }

    [Fact]
    public void AverageMetric_WithEmptyCollection_ShouldThrow()
    {
        // Arrange
        var items = new List<VatRateDto>();
        var selector = new Func<VatRateDto, decimal>(v => v.Percentage);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => items.Average(selector));
    }

    [Fact]
    public void CountMetric_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var items = new List<VatRateDto>();

        // Act
        var count = items.Count();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void FormatMetric_N0_ShouldFormatWithoutDecimals()
    {
        // Arrange
        var value = 42.567m;
        var format = "N0";

        // Act
        var formatted = value.ToString(format);

        // Assert
        Assert.Equal("43", formatted); // Rounded
    }

    [Fact]
    public void FormatMetric_N2_ShouldFormatWithTwoDecimals()
    {
        // Arrange
        var value = 42.567m;
        var format = "N2";

        // Act
        var formatted = value.ToString(format);

        // Assert
        Assert.Equal("42.57", formatted); // Rounded to 2 decimals
    }

    // Test for FilterDefinition removed - type not yet implemented
    // TODO: Re-enable when DashboardFilterDefinition is implemented

    [Fact]
    public void DashboardMetric_WithExpressionSelector_ShouldBeCompilable()
    {
        // Arrange
        var metric = new DashboardMetric<VatRateDto>
        {
            Title = "Average Percentage",
            Type = MetricType.Average,
            ValueSelector = v => v.Percentage
        };

        // Act
        var compiledSelector = metric.ValueSelector!.Compile();
        var testVatRate = _testVatRates.First();
        var value = compiledSelector(testVatRate);

        // Assert
        Assert.Equal(testVatRate.Percentage, value);
    }

    [Fact]
    public void DashboardMetric_WithGroupBySelector_ShouldBeCompilable()
    {
        // Arrange
        var metric = new DashboardMetric<VatRateDto>
        {
            Title = "Count by Status",
            Type = MetricType.Count,
            GroupBySelector = v => v.Status
        };

        // Act
        var compiledGroupBy = metric.GroupBySelector!.Compile();
        var testVatRate = _testVatRates.First();
        var groupValue = compiledGroupBy(testVatRate);

        // Assert
        Assert.Equal(testVatRate.Status, groupValue);
    }
}
