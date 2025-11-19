using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Dashboard;

/// <summary>
/// Represents a single metric configuration within a dashboard.
/// </summary>
public class DashboardMetricConfig : AuditableEntity
{
    /// <summary>
    /// Dashboard configuration ID this metric belongs to.
    /// </summary>
    [Required]
    [Display(Name = "Dashboard Configuration ID", Description = "Dashboard configuration ID this metric belongs to.")]
    public Guid DashboardConfigurationId { get; set; }

    /// <summary>
    /// Display title for the metric.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
    [Display(Name = "Title", Description = "Display title for the metric.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Type of metric calculation.
    /// </summary>
    [Required]
    [Display(Name = "Type", Description = "Type of metric calculation.")]
    public MetricType Type { get; set; }

    /// <summary>
    /// Field/property name to evaluate.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Field name cannot exceed 100 characters.")]
    [Display(Name = "Field Name", Description = "Field/property name to evaluate.")]
    public string? FieldName { get; set; }

    /// <summary>
    /// Optional filter condition.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Filter condition cannot exceed 500 characters.")]
    [Display(Name = "Filter Condition", Description = "Optional filter condition.")]
    public string? FilterCondition { get; set; }

    /// <summary>
    /// Display format string.
    /// </summary>
    [MaxLength(20, ErrorMessage = "Format cannot exceed 20 characters.")]
    [Display(Name = "Format", Description = "Display format string.")]
    public string? Format { get; set; }

    /// <summary>
    /// Icon name (MudBlazor icon).
    /// </summary>
    [MaxLength(100, ErrorMessage = "Icon cannot exceed 100 characters.")]
    [Display(Name = "Icon", Description = "Icon name (MudBlazor icon).")]
    public string? Icon { get; set; }

    /// <summary>
    /// Color for display (MudBlazor color name).
    /// </summary>
    [MaxLength(50, ErrorMessage = "Color cannot exceed 50 characters.")]
    [Display(Name = "Color", Description = "Color for display.")]
    public string? Color { get; set; }

    /// <summary>
    /// Optional description or tooltip.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Optional description or tooltip.")]
    public string? Description { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    [Display(Name = "Order", Description = "Display order.")]
    public int Order { get; set; }

    /// <summary>
    /// Navigation property: The dashboard configuration this metric belongs to.
    /// </summary>
    public virtual DashboardConfiguration DashboardConfiguration { get; set; } = null!;
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
