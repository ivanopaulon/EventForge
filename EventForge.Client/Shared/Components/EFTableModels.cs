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
}

/// <summary>
/// User preferences for EFTable
/// </summary>
public class EFTablePreferences
{
    public Dictionary<string, int> ColumnOrders { get; set; } = new();
    public Dictionary<string, bool> ColumnVisibility { get; set; } = new();
    public List<string> GroupByProperties { get; set; } = new();
}

/// <summary>
/// Result from column configuration dialog
/// </summary>
public class EFTableColumnConfigurationResult
{
    public List<EFTableColumnConfiguration> Columns { get; set; } = new();
    public List<string> GroupByProperties { get; set; } = new();
}
