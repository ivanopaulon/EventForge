namespace EventForge.Client.Shared.Components.Dialogs.UnifiedInventoryDialog;

/// <summary>
/// Represents the different views/steps in the unified inventory dialog
/// </summary>
public enum InventoryDialogView
{
    /// <summary>
    /// View mode - displays product information
    /// </summary>
    View,
    
    /// <summary>
    /// Edit mode - allows editing quantity and location
    /// </summary>
    Edit,
    
    /// <summary>
    /// Confirm mode - reviews changes before saving
    /// </summary>
    Confirm,
    
    /// <summary>
    /// History mode - shows inventory history for the product
    /// </summary>
    History
}
