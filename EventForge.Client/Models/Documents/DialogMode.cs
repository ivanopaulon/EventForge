namespace EventForge.Client.Models.Documents;

/// <summary>
/// Represents the operating mode of the AddDocumentRowDialog
/// </summary>
public enum DialogMode
{
    /// <summary>
    /// Standard mode with all fields and full functionality
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Quick add mode with essential fields only
    /// </summary>
    QuickAdd = 1,

    /// <summary>
    /// Continuous scan mode for rapid barcode scanning
    /// </summary>
    ContinuousScan = 2
}
