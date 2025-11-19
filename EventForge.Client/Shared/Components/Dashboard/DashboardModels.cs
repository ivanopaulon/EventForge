using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EventForge.Client.Shared.Components.Dashboard
{
    /// <summary>
    /// Represents the type of metric calculation to perform.
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// Count the number of items.
        /// </summary>
        Count,

        /// <summary>
        /// Sum a numeric property.
        /// </summary>
        Sum,

        /// <summary>
        /// Calculate average of a numeric property.
        /// </summary>
        Average,

        /// <summary>
        /// Find minimum value of a numeric property.
        /// </summary>
        Min,

        /// <summary>
        /// Find maximum value of a numeric property.
        /// </summary>
        Max
    }

    /// <summary>
    /// Represents the type of filter input control.
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Text input field.
        /// </summary>
        Text,

        /// <summary>
        /// Select dropdown.
        /// </summary>
        Select,

        /// <summary>
        /// Date picker.
        /// </summary>
        Date,

        /// <summary>
        /// Date range picker.
        /// </summary>
        DateRange,

        /// <summary>
        /// Checkbox.
        /// </summary>
        Checkbox,

        /// <summary>
        /// Number input.
        /// </summary>
        Number
    }

    /// <summary>
    /// Represents a metric configuration for the dashboard.
    /// </summary>
    /// <typeparam name="TItem">The type of items being measured.</typeparam>
    public class DashboardMetric<TItem>
    {
        /// <summary>
        /// Display name of the metric.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of metric calculation.
        /// </summary>
        public MetricType Type { get; set; }

        /// <summary>
        /// Property selector for Sum, Average, Min, Max operations.
        /// </summary>
        public Expression<Func<TItem, decimal>>? ValueSelector { get; set; }

        /// <summary>
        /// Optional property selector for grouping.
        /// </summary>
        public Expression<Func<TItem, object>>? GroupBySelector { get; set; }

        /// <summary>
        /// Optional filter to apply before calculation.
        /// </summary>
        public Func<TItem, bool>? Filter { get; set; }

        /// <summary>
        /// Number of top items to show when grouped (0 = all).
        /// </summary>
        public int TopN { get; set; }

        /// <summary>
        /// Format string for displaying the value (e.g., "N2", "C2", "P2").
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Icon to display with the metric (MudBlazor icon).
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Color for the metric display (MudBlazor Color).
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Optional description or tooltip text.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether to show a mini chart for this metric.
        /// </summary>
        public bool ShowChart { get; set; }
    }

    /// <summary>
    /// Represents a calculated metric value.
    /// </summary>
    public class MetricResult
    {
        /// <summary>
        /// Display name of the metric.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Calculated value.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Formatted display value.
        /// </summary>
        public string FormattedValue { get; set; } = string.Empty;

        /// <summary>
        /// Icon to display.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Color for display.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Description or tooltip.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Group label if grouped.
        /// </summary>
        public string? GroupLabel { get; set; }

        /// <summary>
        /// Whether to show a chart.
        /// </summary>
        public bool ShowChart { get; set; }

        /// <summary>
        /// Chart data for mini charts.
        /// </summary>
        public List<ChartDataPoint>? ChartData { get; set; }
    }

    /// <summary>
    /// Represents a data point for charts.
    /// </summary>
    public class ChartDataPoint
    {
        /// <summary>
        /// Label for the data point.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Value of the data point.
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Request for server-side metric calculation.
    /// </summary>
    public class ServerMetricRequest
    {
        /// <summary>
        /// Metrics to calculate.
        /// </summary>
        public List<string> MetricIds { get; set; } = new();

        /// <summary>
        /// Current filter values.
        /// </summary>
        public Dictionary<string, object?> Filters { get; set; } = new();
    }

    /// <summary>
    /// Response from server-side metric calculation.
    /// </summary>
    public class ServerMetricResponse
    {
        /// <summary>
        /// Calculated metrics.
        /// </summary>
        public List<MetricResult> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Represents a filter definition for the dashboard.
    /// </summary>
    public class DashboardFilterDefinition
    {
        /// <summary>
        /// Unique identifier for the filter.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display label for the filter.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Type of filter control.
        /// </summary>
        public FilterType Type { get; set; }

        /// <summary>
        /// Options for select filters.
        /// </summary>
        public List<FilterOption>? Options { get; set; }

        /// <summary>
        /// Default value for the filter.
        /// </summary>
        public string? DefaultValue { get; set; }
    }

    /// <summary>
    /// Represents an option in a select filter.
    /// </summary>
    public class FilterOption
    {
        /// <summary>
        /// Value of the option.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Display label of the option.
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a collection of filter values.
    /// </summary>
    public class DashboardFilters
    {
        private readonly Dictionary<string, object?> _filters = new();

        /// <summary>
        /// Sets a filter value.
        /// </summary>
        public void SetValue(string key, object? value)
        {
            _filters[key] = value;
        }

        /// <summary>
        /// Gets a filter value with type conversion.
        /// </summary>
        public T? GetValue<T>(string key)
        {
            if (!_filters.TryGetValue(key, out var value))
                return default;

            if (value == null)
                return default;

            try
            {
                if (value is T typedValue)
                    return typedValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}
