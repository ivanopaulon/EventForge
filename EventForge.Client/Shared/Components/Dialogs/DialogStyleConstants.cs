using MudBlazor;

namespace EventForge.Client.Shared.Components.Dialogs;

/// <summary>
/// Standardized dialog configuration constants for consistency across the application.
/// Ensures all dialogs have uniform appearance, behavior, and user experience.
/// </summary>
public static class DialogStyleConstants
{
    /// <summary>
    /// Standard dialog options for product-related dialogs
    /// </summary>
    public static class ProductDialogs
    {
        /// <summary>
        /// Options for QuickCreateProductDialog (create/edit product).
        /// Used when creating or editing a product with basic fields (name, price, VAT).
        /// </summary>
        public static DialogOptions QuickCreate => new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = false,
            Position = DialogPosition.Center
        };

        /// <summary>
        /// Options for AdvancedQuickCreateProductDialog (with units of measure).
        /// Used when creating a product with alternative units and multiple barcodes.
        /// </summary>
        public static DialogOptions AdvancedCreate => new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = false,
            Position = DialogPosition.Center
        };

        /// <summary>
        /// Options for ProductNotFoundDialog.
        /// Used when a barcode is scanned but no product is found.
        /// Offers options to skip, create new product, or assign to existing.
        /// CHANGED: MaxWidth from Small to Medium for consistency.
        /// </summary>
        public static DialogOptions NotFound => new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = false,
            Position = DialogPosition.Center
        };
    }

    /// <summary>
    /// Standard dialog options for document-related dialogs
    /// </summary>
    public static class DocumentDialogs
    {
        /// <summary>
        /// Options for AddDocumentRowDialog (add mode).
        /// Used when adding a new row to a document with full fields.
        /// </summary>
        public static DialogOptions AddRow => new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = false,
            Position = DialogPosition.Center
        };

        /// <summary>
        /// Options for AddDocumentRowDialog (edit mode).
        /// Used when editing an existing document row.
        /// Slightly larger to accommodate all fields comfortably.
        /// </summary>
        public static DialogOptions EditRow => new()
        {
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true,
            BackdropClick = false,
            Position = DialogPosition.Center
        };
    }

    /// <summary>
    /// Common icons for dialog titles - ensures consistency
    /// </summary>
    public static class Icons
    {
        public const string Add = MudBlazor.Icons.Material.Outlined.Add;
        public const string AddRow = MudBlazor.Icons.Material.Outlined.PlaylistAdd;
        public const string Edit = MudBlazor.Icons.Material.Outlined.Edit;
        public const string Search = MudBlazor.Icons.Material.Outlined.Search;
        public const string SearchOff = MudBlazor.Icons.Material.Outlined.SearchOff;
        public const string Warning = MudBlazor.Icons.Material.Outlined.Warning;
        public const string Info = MudBlazor.Icons.Material.Outlined.Info;
        public const string Inventory = MudBlazor.Icons.Material.Outlined.Inventory;
        public const string Save = MudBlazor.Icons.Material.Outlined.Save;
        public const string Cancel = MudBlazor.Icons.Material.Outlined.Cancel;
        public const string Delete = MudBlazor.Icons.Material.Outlined.Delete;
        public const string Close = MudBlazor.Icons.Material.Outlined.Close;
    }

    /// <summary>
    /// CSS classes for dialog styling - references dialogs.css
    /// </summary>
    public static class Classes
    {
        public const string DialogTitle = "ef-dialog-title";
        public const string DialogContent = "ef-dialog-content";
        public const string DialogActions = "ef-dialog-actions";
        public const string DialogForm = "ef-dialog-form";
        public const string DialogLoading = "ef-dialog-loading";
        public const string DialogSectionHeader = "ef-dialog-section-header";
    }
}
