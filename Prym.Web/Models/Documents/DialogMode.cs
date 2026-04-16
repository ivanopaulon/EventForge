namespace Prym.Web.Models.Documents;

/// <summary>
/// Represents the operating mode of the DocumentRowDialog
/// </summary>
public enum DialogMode
{
    /// <summary>
    /// Standard mode with all fields and full functionality
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Continuous scan mode for rapid barcode scanning
    /// </summary>
    ContinuousScan = 1
}
