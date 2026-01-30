namespace EventForge.Server.Services.Export;

/// <summary>
/// Service for exporting data to various formats (Excel, CSV)
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports data to Excel format using EPPlus
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="sheetName">Excel sheet name (default: "Data")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Data",
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Exports data to CSV format
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>CSV file as byte array</returns>
    Task<byte[]> ExportToCsvAsync<T>(
        IEnumerable<T> data,
        CancellationToken ct = default) where T : class;
}
