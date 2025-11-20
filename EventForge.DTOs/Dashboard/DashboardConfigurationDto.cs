using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Dashboard
{
    /// <summary>
    /// DTO for dashboard configuration.
    /// </summary>
    public class DashboardConfigurationDto
    {
        /// <summary>
        /// Unique identifier for the configuration.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the configuration.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Entity type this configuration applies to (e.g., "VatRate", "Product").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the default configuration for the entity type.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// List of metric configurations.
        /// </summary>
        public List<DashboardMetricConfigDto> Metrics { get; set; } = new List<DashboardMetricConfigDto>();

        /// <summary>
        /// User ID who owns this configuration.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// When the configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the configuration was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new dashboard configuration.
    /// </summary>
    public class CreateDashboardConfigurationDto
    {
        /// <summary>
        /// Name of the configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the configuration.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Entity type this configuration applies to.
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the default configuration for the entity type.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// List of metric configurations.
        /// </summary>
        public List<DashboardMetricConfigDto> Metrics { get; set; } = new List<DashboardMetricConfigDto>();
    }

    /// <summary>
    /// DTO for updating a dashboard configuration.
    /// </summary>
    public class UpdateDashboardConfigurationDto
    {
        /// <summary>
        /// Name of the configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the configuration.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this is the default configuration for the entity type.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// List of metric configurations.
        /// </summary>
        public List<DashboardMetricConfigDto> Metrics { get; set; } = new List<DashboardMetricConfigDto>();
    }

    /// <summary>
    /// DTO for a single metric configuration.
    /// </summary>
    public class DashboardMetricConfigDto
    {
        /// <summary>
        /// Display title for the metric.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of metric calculation.
        /// </summary>
        public MetricType Type { get; set; }

        /// <summary>
        /// Field/property name to evaluate.
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// Optional filter condition (e.g., "Status == 'Active'").
        /// </summary>
        public string? FilterCondition { get; set; }

        /// <summary>
        /// Display format string (e.g., "N2", "C2", "P2").
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Icon name (MudBlazor icon).
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Icon cannot exceed 1000 characters.")]
        public string? Icon { get; set; }

        /// <summary>
        /// Color for display (MudBlazor color name).
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Optional description or tooltip.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Display order.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Enum for metric calculation types.
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// Count the number of items.
        /// </summary>
        Count = 0,

        /// <summary>
        /// Sum a numeric property.
        /// </summary>
        Sum = 1,

        /// <summary>
        /// Calculate average of a numeric property.
        /// </summary>
        Average = 2,

        /// <summary>
        /// Find minimum value of a numeric property.
        /// </summary>
        Min = 3,

        /// <summary>
        /// Find maximum value of a numeric property.
        /// </summary>
        Max = 4
    }
}
