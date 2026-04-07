using EventForge.Client.Shared.Components;

namespace EventForge.Tests.Components;

/// <summary>
/// Unit tests for EFTable component models and functionality.
/// Note: Blazor component UI tests require bUnit which is not included in this test suite.
/// These tests cover the model classes and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class EFTableTests
{
    #region EFTableColumnConfiguration Tests

    [Fact]
    public void EFTableColumnConfiguration_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new EFTableColumnConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.PropertyName);
        Assert.Equal(string.Empty, config.DisplayName);
        Assert.True(config.IsVisible);
        Assert.Equal(0, config.Order);
    }

    [Fact]
    public void EFTableColumnConfiguration_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var config = new EFTableColumnConfiguration
        {
            PropertyName = "Name",
            DisplayName = "Product Name",
            IsVisible = false,
            Order = 5
        };

        // Assert
        Assert.Equal("Name", config.PropertyName);
        Assert.Equal("Product Name", config.DisplayName);
        Assert.False(config.IsVisible);
        Assert.Equal(5, config.Order);
    }

    #endregion

    #region EFTablePreferences Tests

    [Fact]
    public void EFTablePreferences_ShouldInitializeWithEmptyCollections()
    {
        // Arrange & Act
        var prefs = new EFTablePreferences();

        // Assert
        Assert.NotNull(prefs.ColumnOrders);
        Assert.Empty(prefs.ColumnOrders);
        Assert.NotNull(prefs.ColumnVisibility);
        Assert.Empty(prefs.ColumnVisibility);
        Assert.NotNull(prefs.GroupByProperties);
        Assert.Empty(prefs.GroupByProperties);
    }

    [Fact]
    public void EFTablePreferences_ShouldStoreColumnOrders()
    {
        // Arrange
        var prefs = new EFTablePreferences();

        // Act
        prefs.ColumnOrders["Name"] = 0;
        prefs.ColumnOrders["Price"] = 1;
        prefs.ColumnOrders["Stock"] = 2;

        // Assert
        Assert.Equal(3, prefs.ColumnOrders.Count);
        Assert.Equal(0, prefs.ColumnOrders["Name"]);
        Assert.Equal(1, prefs.ColumnOrders["Price"]);
        Assert.Equal(2, prefs.ColumnOrders["Stock"]);
    }

    [Fact]
    public void EFTablePreferences_ShouldStoreColumnVisibility()
    {
        // Arrange
        var prefs = new EFTablePreferences();

        // Act
        prefs.ColumnVisibility["Name"] = true;
        prefs.ColumnVisibility["Price"] = false;
        prefs.ColumnVisibility["Stock"] = true;

        // Assert
        Assert.Equal(3, prefs.ColumnVisibility.Count);
        Assert.True(prefs.ColumnVisibility["Name"]);
        Assert.False(prefs.ColumnVisibility["Price"]);
        Assert.True(prefs.ColumnVisibility["Stock"]);
    }

    [Fact]
    public void EFTablePreferences_ShouldStoreGroupByProperties()
    {
        // Arrange
        var prefs = new EFTablePreferences();

        // Act
        prefs.GroupByProperties.Add("Category");
        prefs.GroupByProperties.Add("Status");

        // Assert
        Assert.Equal(2, prefs.GroupByProperties.Count);
        Assert.Equal("Category", prefs.GroupByProperties[0]);
        Assert.Equal("Status", prefs.GroupByProperties[1]);
    }

    #endregion

    #region EFTableColumnConfigurationResult Tests

    [Fact]
    public void EFTableColumnConfigurationResult_ShouldInitializeWithEmptyCollections()
    {
        // Arrange & Act
        var result = new EFTableColumnConfigurationResult();

        // Assert
        Assert.NotNull(result.Columns);
        Assert.Empty(result.Columns);
        Assert.NotNull(result.GroupByProperties);
        Assert.Empty(result.GroupByProperties);
    }

    [Fact]
    public void EFTableColumnConfigurationResult_ShouldStoreColumnsAndGrouping()
    {
        // Arrange & Act
        var result = new EFTableColumnConfigurationResult
        {
            Columns = new List<EFTableColumnConfiguration>
            {
                new() { PropertyName = "Name", DisplayName = "Name", IsVisible = true, Order = 0 },
                new() { PropertyName = "Price", DisplayName = "Price", IsVisible = false, Order = 1 }
            },
            GroupByProperties = new List<string> { "Category" }
        };

        // Assert
        Assert.Equal(2, result.Columns.Count);
        Assert.Single(result.GroupByProperties);
        Assert.Equal("Category", result.GroupByProperties[0]);
    }

    #endregion

    #region EFTableAction Tests

    [Fact]
    public void EFTableAction_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var action = new EFTableAction();

        // Assert
        Assert.Equal(string.Empty, action.Id);
        Assert.Equal(string.Empty, action.Label);
        Assert.Null(action.Icon);
        Assert.Null(action.Color);
        Assert.False(action.RequiresSelection);
        Assert.True(action.IsEnabled);
        Assert.Null(action.Tooltip);
    }

    [Fact]
    public void EFTableAction_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var action = new EFTableAction
        {
            Id = "export",
            Label = "Export Data",
            Icon = "download",
            Color = "Primary",
            RequiresSelection = true,
            IsEnabled = false,
            Tooltip = "Export selected items"
        };

        // Assert
        Assert.Equal("export", action.Id);
        Assert.Equal("Export Data", action.Label);
        Assert.Equal("download", action.Icon);
        Assert.Equal("Primary", action.Color);
        Assert.True(action.RequiresSelection);
        Assert.False(action.IsEnabled);
        Assert.Equal("Export selected items", action.Tooltip);
    }

    [Fact]
    public void EFTableAction_RequiresSelection_ShouldIndicateIfItemsMustBeSelected()
    {
        // Arrange
        var bulkAction = new EFTableAction { RequiresSelection = true };
        var generalAction = new EFTableAction { RequiresSelection = false };

        // Assert
        Assert.True(bulkAction.RequiresSelection);
        Assert.False(generalAction.RequiresSelection);
    }

    #endregion

    #region EFTableActionEventArgs Tests

    [Fact]
    public void EFTableActionEventArgs_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var args = new EFTableActionEventArgs();

        // Assert
        Assert.Equal(string.Empty, args.ActionId);
        Assert.Null(args.Payload);
    }

    [Fact]
    public void EFTableActionEventArgs_ShouldStoreActionIdAndPayload()
    {
        // Arrange
        var selectedItems = new List<string> { "item1", "item2" };

        // Act
        var args = new EFTableActionEventArgs
        {
            ActionId = "delete",
            Payload = selectedItems
        };

        // Assert
        Assert.Equal("delete", args.ActionId);
        Assert.NotNull(args.Payload);
        Assert.IsType<List<string>>(args.Payload);
        var payload = args.Payload as List<string>;
        Assert.Equal(2, payload!.Count);
    }

    [Fact]
    public void EFTableActionEventArgs_PayloadCanBeNull()
    {
        // Arrange & Act
        var args = new EFTableActionEventArgs
        {
            ActionId = "refresh",
            Payload = null
        };

        // Assert
        Assert.Equal("refresh", args.ActionId);
        Assert.Null(args.Payload);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EFTable_ColumnConfigurationWorkflow_ShouldMaintainState()
    {
        // Arrange - Initial configuration
        var initialColumns = new List<EFTableColumnConfiguration>
        {
            new() { PropertyName = "Name", DisplayName = "Product Name", IsVisible = true, Order = 0 },
            new() { PropertyName = "Price", DisplayName = "Price", IsVisible = true, Order = 1 },
            new() { PropertyName = "Stock", DisplayName = "Stock", IsVisible = false, Order = 2 }
        };

        // Act - User modifies configuration
        var modifiedColumns = initialColumns.Select(c => new EFTableColumnConfiguration
        {
            PropertyName = c.PropertyName,
            DisplayName = c.DisplayName,
            IsVisible = c.PropertyName != "Stock", // Hide Stock column
            Order = c.Order
        }).ToList();

        // Simulate saving to preferences
        var preferences = new EFTablePreferences
        {
            ColumnOrders = modifiedColumns.ToDictionary(c => c.PropertyName, c => c.Order),
            ColumnVisibility = modifiedColumns.ToDictionary(c => c.PropertyName, c => c.IsVisible)
        };

        // Assert - Configuration is properly saved
        Assert.Equal(3, preferences.ColumnOrders.Count);
        Assert.Equal(3, preferences.ColumnVisibility.Count);
        Assert.True(preferences.ColumnVisibility["Name"]);
        Assert.True(preferences.ColumnVisibility["Price"]);
        Assert.False(preferences.ColumnVisibility["Stock"]);
    }

    [Fact]
    public void EFTable_GroupingWorkflow_ShouldMaintainGroupHierarchy()
    {
        // Arrange
        var preferences = new EFTablePreferences();

        // Act - User adds multi-level grouping
        preferences.GroupByProperties.Add("Category");
        preferences.GroupByProperties.Add("Status");
        preferences.GroupByProperties.Add("Supplier");

        // Assert
        Assert.Equal(3, preferences.GroupByProperties.Count);
        Assert.Equal("Category", preferences.GroupByProperties[0]); // First level
        Assert.Equal("Status", preferences.GroupByProperties[1]);   // Second level
        Assert.Equal("Supplier", preferences.GroupByProperties[2]); // Third level
    }

    [Fact]
    public void EFTable_ActionWorkflow_ShouldSupportMultipleActions()
    {
        // Arrange
        var actions = new List<EFTableAction>
        {
            new()
            {
                Id = "export",
                Label = "Export",
                Icon = "download",
                RequiresSelection = false,
                IsEnabled = true
            },
            new()
            {
                Id = "bulk-delete",
                Label = "Delete Selected",
                Icon = "delete",
                RequiresSelection = true,
                IsEnabled = true
            },
            new()
            {
                Id = "archive",
                Label = "Archive",
                Icon = "archive",
                RequiresSelection = false,
                IsEnabled = false
            }
        };

        // Act - Filter to enabled actions
        var enabledActions = actions.Where(a => a.IsEnabled).ToList();

        // Assert
        Assert.Equal(3, actions.Count);
        Assert.Equal(2, enabledActions.Count);
        Assert.Contains(enabledActions, a => a.Id == "export");
        Assert.Contains(enabledActions, a => a.Id == "bulk-delete");
        Assert.DoesNotContain(enabledActions, a => a.Id == "archive");
    }

    [Fact]
    public void EFTable_ActionWorkflow_ShouldFilterBySelectionRequirement()
    {
        // Arrange
        var actions = new List<EFTableAction>
        {
            new() { Id = "add", Label = "Add", RequiresSelection = false },
            new() { Id = "delete", Label = "Delete", RequiresSelection = true },
            new() { Id = "export", Label = "Export", RequiresSelection = false }
        };

        bool hasSelection = false;

        // Act - Filter actions when no items are selected
        var availableActions = actions.Where(a => !a.RequiresSelection || hasSelection).ToList();

        // Assert
        Assert.Equal(2, availableActions.Count);
        Assert.Contains(availableActions, a => a.Id == "add");
        Assert.Contains(availableActions, a => a.Id == "export");
        Assert.DoesNotContain(availableActions, a => a.Id == "delete");
    }

    #endregion

    #region Column Ordering Tests

    [Fact]
    public void EFTable_ColumnOrdering_ShouldSortByOrderProperty()
    {
        // Arrange
        var columns = new List<EFTableColumnConfiguration>
        {
            new() { PropertyName = "Price", Order = 2 },
            new() { PropertyName = "Name", Order = 0 },
            new() { PropertyName = "Stock", Order = 1 }
        };

        // Act
        var orderedColumns = columns.OrderBy(c => c.Order).ToList();

        // Assert
        Assert.Equal("Name", orderedColumns[0].PropertyName);
        Assert.Equal("Stock", orderedColumns[1].PropertyName);
        Assert.Equal("Price", orderedColumns[2].PropertyName);
    }

    [Fact]
    public void EFTable_ColumnVisibility_ShouldFilterInvisibleColumns()
    {
        // Arrange
        var columns = new List<EFTableColumnConfiguration>
        {
            new() { PropertyName = "Name", IsVisible = true },
            new() { PropertyName = "Price", IsVisible = false },
            new() { PropertyName = "Stock", IsVisible = true }
        };

        // Act
        var visibleColumns = columns.Where(c => c.IsVisible).ToList();

        // Assert
        Assert.Equal(2, visibleColumns.Count);
        Assert.Contains(visibleColumns, c => c.PropertyName == "Name");
        Assert.Contains(visibleColumns, c => c.PropertyName == "Stock");
        Assert.DoesNotContain(visibleColumns, c => c.PropertyName == "Price");
    }

    #endregion
}
