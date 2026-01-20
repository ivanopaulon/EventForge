namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Constants for DocumentRow Dialog operations
/// </summary>
public static class DocumentRowDialogConstants
{
    /// <summary>
    /// Delay constants for UI operations
    /// </summary>
    public static class Delays
    {
        /// <summary>
        /// Delay in milliseconds for UI rendering after state changes
        /// </summary>
        public const int RenderDelayMs = 100;

        /// <summary>
        /// Delay in milliseconds for refocusing UI elements
        /// </summary>
        public const int RefocusDelayMs = 100;

        /// <summary>
        /// Debounce delay for LocalStorage write operations
        /// </summary>
        public const int DebounceSaveMs = 500;
    }

    /// <summary>
    /// Limit constants for collections
    /// </summary>
    public static class Limits
    {
        /// <summary>
        /// Maximum number of recent scans to keep in continuous scan mode
        /// </summary>
        public const int MaxRecentScans = 20;

        /// <summary>
        /// Maximum number of recent quick entries to track
        /// </summary>
        public const int MaxRecentQuickEntries = 10;
    }

    /// <summary>
    /// LocalStorage key constants
    /// </summary>
    public static class LocalStorageKeys
    {
        /// <summary>
        /// Key for storing panel expansion states
        /// </summary>
        public const string PanelStates = "EventForge.Documents.AddDocumentRowDialog.PanelStates";
    }

    /// <summary>
    /// Document type keywords for transaction type detection
    /// </summary>
    public static class DocumentTypeKeywords
    {
        /// <summary>
        /// Keywords that indicate purchase-type documents
        /// </summary>
        public static readonly string[] Purchase = 
            { "purchase", "receipt", "return", "acquisto", "carico", "reso" };

        /// <summary>
        /// Keywords that indicate sale-type documents
        /// </summary>
        public static readonly string[] Sale = 
            { "sale", "invoice", "shipment", "delivery", "vendita", "fattura", "scarico", "consegna" };
    }
}
