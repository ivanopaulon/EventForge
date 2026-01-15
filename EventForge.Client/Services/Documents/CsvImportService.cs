using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Service for importing document rows from CSV files
/// </summary>
public class CsvImportService : ICsvImportService
{
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(ILogger<CsvImportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CsvImportResult> ImportFromCsvAsync(Stream csvStream, CsvImportOptions options)
    {
        var result = new CsvImportResult
        {
            ValidRows = new List<CreateDocumentRowDto>(),
            InvalidRows = new List<CsvImportError>()
        };

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            
            // Skip header row
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new InvalidOperationException("CSV file is empty or has no header");
            }

            var headers = ParseCsvLine(headerLine);
            var columnMapping = MapHeaders(headers, options);

            int rowNumber = 1; // Start from 1 (header is row 0)
            
            while (!reader.EndOfStream)
            {
                rowNumber++;
                var line = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = ParseCsvLine(line);
                
                try
                {
                    var row = ParseRow(values, columnMapping, options);
                    result.ValidRows.Add(row);
                }
                catch (Exception ex)
                {
                    result.InvalidRows.Add(new CsvImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = ex.Message,
                        RawData = line
                    });
                    _logger.LogWarning(ex, "Error parsing CSV row {RowNumber}", rowNumber);
                }
            }

            _logger.LogInformation(
                "CSV import completed: {ValidCount} valid rows, {InvalidCount} invalid rows",
                result.ValidRows.Count,
                result.InvalidRows.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV file");
            throw;
        }
    }

    /// <summary>
    /// Parses a CSV line respecting quoted values
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString().Trim());
        return values;
    }

    /// <summary>
    /// Maps CSV headers to column indices
    /// </summary>
    private Dictionary<string, int> MapHeaders(List<string> headers, CsvImportOptions options)
    {
        var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            
            // Map common header names
            if (IsProductCodeHeader(header))
                mapping["ProductCode"] = i;
            else if (IsDescriptionHeader(header))
                mapping["Description"] = i;
            else if (IsQuantityHeader(header))
                mapping["Quantity"] = i;
            else if (IsUnitPriceHeader(header))
                mapping["UnitPrice"] = i;
            else if (IsVatRateHeader(header))
                mapping["VatRate"] = i;
            else if (IsUnitOfMeasureHeader(header))
                mapping["UnitOfMeasure"] = i;
        }

        return mapping;
    }

    /// <summary>
    /// Parses a single row into a CreateDocumentRowDto
    /// </summary>
    private CreateDocumentRowDto ParseRow(
        List<string> values,
        Dictionary<string, int> mapping,
        CsvImportOptions options)
    {
        var row = new CreateDocumentRowDto
        {
            DocumentHeaderId = options.DocumentHeaderId,
            Quantity = 1m // Default quantity
        };

        // Product Code
        if (mapping.TryGetValue("ProductCode", out int productCodeIdx) && productCodeIdx < values.Count)
        {
            row.ProductCode = values[productCodeIdx];
            if (string.IsNullOrWhiteSpace(row.ProductCode))
                throw new InvalidOperationException("Product code is required");
        }
        else if (options.RequireProductCode)
        {
            throw new InvalidOperationException("Product code column not found or empty");
        }

        // Description
        if (mapping.TryGetValue("Description", out int descIdx) && descIdx < values.Count)
        {
            row.Description = values[descIdx];
        }

        if (string.IsNullOrWhiteSpace(row.Description))
            row.Description = row.ProductCode; // Fallback to product code

        // Quantity
        if (mapping.TryGetValue("Quantity", out int qtyIdx) && qtyIdx < values.Count)
        {
            if (decimal.TryParse(values[qtyIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity))
            {
                row.Quantity = quantity;
            }
            else if (!string.IsNullOrWhiteSpace(values[qtyIdx]))
            {
                throw new InvalidOperationException($"Invalid quantity value: {values[qtyIdx]}");
            }
        }

        // Unit Price
        if (mapping.TryGetValue("UnitPrice", out int priceIdx) && priceIdx < values.Count)
        {
            if (decimal.TryParse(values[priceIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
            {
                row.UnitPrice = price;
            }
        }

        // VAT Rate
        if (mapping.TryGetValue("VatRate", out int vatIdx) && vatIdx < values.Count)
        {
            if (decimal.TryParse(values[vatIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vatRate))
            {
                row.VatRate = vatRate;
            }
        }

        // Unit of Measure
        if (mapping.TryGetValue("UnitOfMeasure", out int uomIdx) && uomIdx < values.Count)
        {
            row.UnitOfMeasure = values[uomIdx];
        }

        return row;
    }

    #region Header Detection

    private bool IsProductCodeHeader(string header)
    {
        var productCodeVariants = new[] { "productcode", "product code", "code", "codice", "sku", "item code" };
        return productCodeVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsDescriptionHeader(string header)
    {
        var descriptionVariants = new[] { "description", "descrizione", "name", "nome", "product name" };
        return descriptionVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsQuantityHeader(string header)
    {
        var quantityVariants = new[] { "quantity", "quantità", "qty", "amount", "qta" };
        return quantityVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsUnitPriceHeader(string header)
    {
        var priceVariants = new[] { "unitprice", "unit price", "price", "prezzo", "prezzo unitario" };
        return priceVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsVatRateHeader(string header)
    {
        var vatVariants = new[] { "vatrate", "vat rate", "vat", "iva", "tax", "tax rate" };
        return vatVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsUnitOfMeasureHeader(string header)
    {
        var uomVariants = new[] { "unitofmeasure", "unit of measure", "uom", "um", "unit" };
        return uomVariants.Any(v => header.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}

/// <summary>
/// Interface for CSV import service
/// </summary>
public interface ICsvImportService
{
    /// <summary>
    /// Imports document rows from a CSV stream
    /// </summary>
    Task<CsvImportResult> ImportFromCsvAsync(Stream csvStream, CsvImportOptions options);
}

/// <summary>
/// Options for CSV import
/// </summary>
public class CsvImportOptions
{
    /// <summary>
    /// Document header ID to associate imported rows with
    /// </summary>
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Whether product code is required for each row
    /// </summary>
    public bool RequireProductCode { get; set; } = true;

    /// <summary>
    /// Default VAT rate to use if not specified in CSV
    /// </summary>
    public decimal DefaultVatRate { get; set; } = 0m;
}

/// <summary>
/// Result of CSV import operation
/// </summary>
public class CsvImportResult
{
    /// <summary>
    /// Successfully parsed rows
    /// </summary>
    public List<CreateDocumentRowDto> ValidRows { get; set; } = new();

    /// <summary>
    /// Rows that failed to parse
    /// </summary>
    public List<CsvImportError> InvalidRows { get; set; } = new();

    /// <summary>
    /// Total number of rows processed
    /// </summary>
    public int TotalRows => ValidRows.Count + InvalidRows.Count;

    /// <summary>
    /// Whether the import was successful (no invalid rows)
    /// </summary>
    public bool IsSuccess => InvalidRows.Count == 0;
}

/// <summary>
/// Error information for invalid CSV rows
/// </summary>
public class CsvImportError
{
    /// <summary>
    /// Row number in the CSV file
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Raw data from the CSV row
    /// </summary>
    public string RawData { get; set; } = string.Empty;
}
