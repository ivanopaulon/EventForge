namespace EventForge.Client.Shared.Components;

/// <summary>
/// Configuration for a single column in EFTable
/// </summary>
public class EFTableColumnConfiguration
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    
    /// <summary>
    /// Indicates whether this column is searchable via the search bar.
    /// Default is true to maintain backward compatibility.
    /// </summary>
    public bool IsSearchable { get; set; } = true;
}

/// <summary>
/// User preferences for EFTable
/// </summary>
public class EFTablePreferences
{
    public Dictionary<string, int> ColumnOrders { get; set; } = new();
    public Dictionary<string, bool> ColumnVisibility { get; set; } = new();
    public List<string> GroupByProperties { get; set; } = new();
    
    /// <summary>
    /// Stores which columns are searchable (PropertyName -> IsSearchable).
    /// </summary>
    public Dictionary<string, bool> ColumnSearchability { get; set; } = new();
}

/// <summary>
/// Result from column configuration dialog
/// </summary>
public class EFTableColumnConfigurationResult
{
    public List<EFTableColumnConfiguration> Columns { get; set; } = new();
    public List<string> GroupByProperties { get; set; } = new();
}

/// <summary>
/// Represents a custom action in EFTable
/// </summary>
public class EFTableAction
{
    /// <summary>
    /// Unique identifier for the action
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the action
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Icon to display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Color of the action button
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Whether this action requires items to be selected
    /// </summary>
    public bool RequiresSelection { get; set; } = false;

    /// <summary>
    /// Whether this action is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Tooltip text
    /// </summary>
    public string? Tooltip { get; set; }
}

/// <summary>
/// Event args for custom actions
/// </summary>
public class EFTableActionEventArgs
{
    /// <summary>
    /// Action identifier
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Selected items if any
    /// </summary>
    public object? Payload { get; set; }
}
