using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for importing supplier products from CSV files.
/// </summary>
public interface ISupplierProductCsvImportService
{
    /// <summary>
    /// Validates a CSV file before import.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="file">CSV file to validate.</param>
    /// <param name="columnMapping">Optional pre-defined column mapping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with preview and suggestions.</returns>
    Task<CsvValidationResult> ValidateCsvAsync(
        Guid supplierId,
        IFormFile file,
        ColumnMapping? columnMapping = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports supplier products from a CSV file.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="file">CSV file to import.</param>
    /// <param name="options">Import options.</param>
    /// <param name="currentUser">Username of user performing the import.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result with statistics and errors.</returns>
    Task<CsvImportResult> ImportCsvAsync(
        Guid supplierId,
        IFormFile file,
        CsvImportOptions options,
        string currentUser,
        CancellationToken cancellationToken = default);
}
