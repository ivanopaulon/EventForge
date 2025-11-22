using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using EventForge.DTOs.PriceHistory;
using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceHistory;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for importing supplier products from CSV files.
/// </summary>
public class SupplierProductCsvImportService : ISupplierProductCsvImportService
{
    private readonly EventForgeDbContext _context;
    private readonly ISupplierProductPriceHistoryService _priceHistoryService;
    private readonly ILogger<SupplierProductCsvImportService> _logger;
    private readonly IConfiguration _configuration;

    public SupplierProductCsvImportService(
        EventForgeDbContext context,
        ISupplierProductPriceHistoryService priceHistoryService,
        ILogger<SupplierProductCsvImportService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _priceHistoryService = priceHistoryService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<CsvValidationResult> ValidateCsvAsync(
        Guid supplierId,
        IFormFile file,
        ColumnMapping? columnMapping = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvValidationResult
        {
            IsValid = true,
            FileInfo = new CsvFileInfo
            {
                FileName = file.FileName,
                FileSizeBytes = file.Length
            }
        };

        try
        {
            // Check file size
            var maxFileSize = _configuration.GetValue<long>("CsvImport:MaxFileSizeBytes", 10485760);
            if (file.Length > maxFileSize)
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "FileTooLarge",
                    ErrorMessage = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024} MB"
                });
                return result;
            }

            // Read and parse CSV
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            
            // Detect delimiter
            var firstLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(firstLine))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "EmptyFile",
                    ErrorMessage = "CSV file is empty"
                });
                return result;
            }

            var delimiter = DetectDelimiter(firstLine);
            result.FileInfo.Delimiter = delimiter;
            result.FileInfo.Encoding = reader.CurrentEncoding.WebName;

            // Reset stream and parse CSV
            stream.Position = 0;
            using var csvReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var csv = new CsvHelper.CsvReader(csvReader, csvConfig);
            
            // Read header
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();
            result.DetectedColumns = headers.ToList();

            // Auto-detect column mapping
            result.SuggestedMapping = columnMapping ?? AutoDetectColumnMapping(headers);

            // Validate that required columns are mapped
            if (string.IsNullOrEmpty(result.SuggestedMapping.ProductCodeColumn))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "MissingRequiredColumn",
                    ErrorMessage = "Could not detect ProductCode column. Please map it manually."
                });
            }

            if (string.IsNullOrEmpty(result.SuggestedMapping.UnitCostColumn))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "MissingRequiredColumn",
                    ErrorMessage = "Could not detect UnitCost column. Please map it manually."
                });
            }

            // Read preview rows
            var maxPreviewRows = _configuration.GetValue<int>("CsvImport:MaxRowsPreview", 10);
            var rowNumber = 1;
            var totalRows = 0;

            while (await csv.ReadAsync() && rowNumber <= maxPreviewRows)
            {
                totalRows++;
                var previewRow = new CsvPreviewRow
                {
                    RowNumber = rowNumber,
                    Values = new Dictionary<string, string>()
                };

                foreach (var header in headers)
                {
                    previewRow.Values[header] = csv.GetField(header) ?? string.Empty;
                }

                // Validate row
                var rowErrors = ValidateRow(previewRow, result.SuggestedMapping);
                if (rowErrors.Any())
                {
                    previewRow.HasErrors = true;
                    previewRow.ErrorSummary = string.Join("; ", rowErrors.Select(e => e.ErrorMessage));
                    result.ValidationErrors.AddRange(rowErrors);
                }

                result.PreviewRows.Add(previewRow);
                rowNumber++;
            }

            // Count remaining rows
            while (await csv.ReadAsync())
            {
                totalRows++;
            }

            result.FileInfo.TotalRows = totalRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CSV file {FileName}", file.FileName);
            result.IsValid = false;
            result.ValidationErrors.Add(new CsvImportError
            {
                RowNumber = 0,
                ErrorType = "ParseError",
                ErrorMessage = $"Error parsing CSV file: {ex.Message}"
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CsvImportResult> ImportCsvAsync(
        Guid supplierId,
        IFormFile file,
        CsvImportOptions options,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CsvImportResult { Success = true };

        try
        {
            // Get user ID from username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUser, cancellationToken);
            var userId = user?.Id ?? Guid.Empty;

            // Verify supplier exists
            var supplierExists = await _context.BusinessParties
                .AnyAsync(bp => bp.Id == supplierId, cancellationToken);

            if (!supplierExists)
            {
                result.Success = false;
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "SupplierNotFound",
                    ErrorMessage = "Supplier not found"
                });
                return result;
            }

            // Parse CSV
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            
            var firstLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(firstLine))
            {
                result.Success = false;
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "EmptyFile",
                    ErrorMessage = "CSV file is empty"
                });
                return result;
            }

            var delimiter = DetectDelimiter(firstLine);
            stream.Position = 0;

            using var csvReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var csv = new CsvHelper.CsvReader(csvReader, csvConfig);
            
            await csv.ReadAsync();
            csv.ReadHeader();

            var batchSize = _configuration.GetValue<int>("CsvImport:BatchSize", 100);
            var rowNumber = 1;
            var priceChanges = new List<PriceChangeLogRequest>();
            decimal totalValue = 0;
            var priceChangesList = new List<decimal>();

            while (await csv.ReadAsync())
            {
                result.TotalRows++;
                
                try
                {
                    var productCode = GetFieldValue(csv, options.ColumnMapping.ProductCodeColumn);
                    var productName = GetFieldValue(csv, options.ColumnMapping.ProductNameColumn);
                    var unitCostStr = GetFieldValue(csv, options.ColumnMapping.UnitCostColumn);
                    var leadTimeDaysStr = GetFieldValue(csv, options.ColumnMapping.LeadTimeDaysColumn);
                    var minOrderQtyStr = GetFieldValue(csv, options.ColumnMapping.MinOrderQuantityColumn);
                    var currency = GetFieldValue(csv, options.ColumnMapping.CurrencyColumn) ?? options.Currency;
                    var notes = GetFieldValue(csv, options.ColumnMapping.NotesColumn);

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(productCode))
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new CsvImportError
                        {
                            RowNumber = rowNumber,
                            ProductCode = productCode,
                            ErrorType = "ValidationError",
                            ErrorMessage = "ProductCode is required"
                        });
                        rowNumber++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(unitCostStr) || !decimal.TryParse(unitCostStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitCost))
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new CsvImportError
                        {
                            RowNumber = rowNumber,
                            ProductCode = productCode,
                            ErrorType = "ValidationError",
                            ErrorMessage = "UnitCost is required and must be a valid number"
                        });
                        rowNumber++;
                        continue;
                    }

                    if (unitCost <= 0)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new CsvImportError
                        {
                            RowNumber = rowNumber,
                            ProductCode = productCode,
                            ErrorType = "ValidationError",
                            ErrorMessage = "UnitCost must be greater than 0"
                        });
                        rowNumber++;
                        continue;
                    }

                    // Parse optional fields
                    int? leadTimeDays = null;
                    if (!string.IsNullOrWhiteSpace(leadTimeDaysStr) && int.TryParse(leadTimeDaysStr, out var ltd))
                    {
                        leadTimeDays = ltd >= 0 ? ltd : null;
                    }

                    int? minOrderQty = null;
                    if (!string.IsNullOrWhiteSpace(minOrderQtyStr) && int.TryParse(minOrderQtyStr, out var moq))
                    {
                        minOrderQty = moq >= 1 ? moq : null;
                    }

                    // Find product by code (case-insensitive)
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Code.ToLower() == productCode.ToLower(), cancellationToken);

                    if (product == null)
                    {
                        if (options.CreateNew && !string.IsNullOrWhiteSpace(productName))
                        {
                            // Create new product
                            var supplier = await _context.BusinessParties.FirstAsync(bp => bp.Id == supplierId, cancellationToken);
                            product = new Product
                            {
                                Id = Guid.NewGuid(),
                                Code = productCode,
                                Name = productName,
                                Status = ProductStatus.Active,
                                TenantId = supplier.TenantId,
                                CreatedAt = DateTime.UtcNow,
                                ModifiedAt = DateTime.UtcNow
                            };
                            _context.Products.Add(product);
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new CsvImportError
                            {
                                RowNumber = rowNumber,
                                ProductCode = productCode,
                                ErrorType = "ProductNotFound",
                                ErrorMessage = options.CreateNew 
                                    ? "Product not found and ProductName is required to create new products"
                                    : "Product not found and CreateNew option is disabled"
                            });
                            rowNumber++;
                            continue;
                        }
                    }

                    // Check if product-supplier relationship exists
                    var productSupplier = await _context.Set<ProductSupplier>()
                        .FirstOrDefaultAsync(ps => ps.ProductId == product.Id && ps.SupplierId == supplierId, cancellationToken);

                    if (productSupplier != null)
                    {
                        // Update existing
                        if (options.UpdateExisting)
                        {
                            var oldPrice = productSupplier.UnitCost ?? 0;
                            var oldLeadTime = productSupplier.LeadTimeDays;

                            productSupplier.UnitCost = unitCost;
                            productSupplier.Currency = currency;
                            productSupplier.LeadTimeDays = leadTimeDays;
                            productSupplier.MinOrderQty = minOrderQty;
                            if (options.SetAsPreferred)
                            {
                                productSupplier.Preferred = true;
                            }
                            if (!string.IsNullOrWhiteSpace(notes))
                            {
                                productSupplier.Notes = notes;
                            }
                            productSupplier.ModifiedAt = DateTime.UtcNow;

                            result.UpdatedCount++;
                            totalValue += unitCost;

                            // Track price change
                            if (oldPrice != unitCost && oldPrice > 0)
                            {
                                var priceChange = ((unitCost - oldPrice) / oldPrice) * 100;
                                priceChangesList.Add(priceChange);

                                priceChanges.Add(new PriceChangeLogRequest
                                {
                                    ProductSupplierId = productSupplier.Id,
                                    SupplierId = supplierId,
                                    ProductId = product.Id,
                                    OldPrice = oldPrice,
                                    NewPrice = unitCost,
                                    Currency = currency,
                                    OldLeadTimeDays = oldLeadTime,
                                    NewLeadTimeDays = leadTimeDays,
                                    ChangeSource = "CSVImport",
                                    ChangeReason = $"Imported from file: {file.FileName}",
                                    UserId = userId
                                });
                            }
                        }
                        else if (options.SkipDuplicates)
                        {
                            result.SkippedCount++;
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new CsvImportError
                            {
                                RowNumber = rowNumber,
                                ProductCode = productCode,
                                ErrorType = "DuplicateEntry",
                                ErrorMessage = "Product-supplier relationship already exists"
                            });
                        }
                    }
                    else
                    {
                        // Create new relationship
                        productSupplier = new ProductSupplier
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            SupplierId = supplierId,
                            UnitCost = unitCost,
                            Currency = currency,
                            LeadTimeDays = leadTimeDays,
                            MinOrderQty = minOrderQty,
                            Preferred = options.SetAsPreferred,
                            Notes = notes,
                            TenantId = product.TenantId,
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow
                        };
                        _context.Set<ProductSupplier>().Add(productSupplier);
                        result.CreatedCount++;
                        totalValue += unitCost;
                    }

                    // Batch save
                    if (rowNumber % batchSize == 0)
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber}", rowNumber);
                    result.ErrorCount++;
                    result.Errors.Add(new CsvImportError
                    {
                        RowNumber = rowNumber,
                        ErrorType = "ProcessingError",
                        ErrorMessage = $"Error processing row: {ex.Message}"
                    });
                }

                rowNumber++;
            }

            // Final save
            await _context.SaveChangesAsync(cancellationToken);

            // Log price changes
            if (priceChanges.Any())
            {
                try
                {
                    await _priceHistoryService.LogBulkPriceChangesAsync(priceChanges, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging price changes");
                }
            }

            // Calculate statistics
            stopwatch.Stop();
            result.Statistics.TotalImportedValue = totalValue;
            result.Statistics.AveragePriceChange = priceChangesList.Any() ? priceChangesList.Average() : 0;
            result.Statistics.ProcessingTime = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV file {FileName}", file.FileName);
            result.Success = false;
            result.Errors.Add(new CsvImportError
            {
                RowNumber = 0,
                ErrorType = "ImportError",
                ErrorMessage = $"Error importing CSV file: {ex.Message}"
            });
        }

        return result;
    }

    private string DetectDelimiter(string firstLine)
    {
        // Count occurrences of common delimiters
        var commaCount = firstLine.Count(c => c == ',');
        var semicolonCount = firstLine.Count(c => c == ';');
        var tabCount = firstLine.Count(c => c == '\t');

        // Return delimiter with highest count
        if (commaCount >= semicolonCount && commaCount >= tabCount)
            return ",";
        if (semicolonCount >= tabCount)
            return ";";
        return "\t";
    }

    private ColumnMapping AutoDetectColumnMapping(string[] headers)
    {
        var mapping = new ColumnMapping();

        foreach (var header in headers)
        {
            var lowerHeader = header.ToLower().Trim();

            // Product Code
            if ((lowerHeader.Contains("product") && lowerHeader.Contains("code")) ||
                lowerHeader.Contains("productcode") ||
                lowerHeader.Contains("sku") ||
                lowerHeader == "code" ||
                lowerHeader == "codice")
            {
                mapping.ProductCodeColumn = header;
            }
            // Product Name
            else if ((lowerHeader.Contains("product") && lowerHeader.Contains("name")) ||
                     lowerHeader.Contains("productname") ||
                     lowerHeader == "name" ||
                     lowerHeader == "nome" ||
                     lowerHeader == "description" ||
                     lowerHeader == "descrizione")
            {
                mapping.ProductNameColumn = header;
            }
            // Unit Cost
            else if (lowerHeader.Contains("unitcost") ||
                     lowerHeader.Contains("unit") && lowerHeader.Contains("cost") ||
                     lowerHeader.Contains("price") ||
                     lowerHeader.Contains("prezzo") ||
                     lowerHeader.Contains("costo"))
            {
                mapping.UnitCostColumn = header;
            }
            // Lead Time
            else if (lowerHeader.Contains("leadtime") ||
                     lowerHeader.Contains("lead") && lowerHeader.Contains("time") ||
                     lowerHeader.Contains("delivery") && lowerHeader.Contains("time"))
            {
                mapping.LeadTimeDaysColumn = header;
            }
            // Min Order Qty
            else if (lowerHeader.Contains("minorder") ||
                     lowerHeader.Contains("min") && lowerHeader.Contains("order") ||
                     lowerHeader.Contains("moq") ||
                     lowerHeader.Contains("minimumquantity"))
            {
                mapping.MinOrderQuantityColumn = header;
            }
            // Currency
            else if (lowerHeader == "currency" ||
                     lowerHeader == "valuta" ||
                     lowerHeader == "curr")
            {
                mapping.CurrencyColumn = header;
            }
            // Notes
            else if (lowerHeader == "notes" ||
                     lowerHeader == "note" ||
                     lowerHeader == "comments" ||
                     lowerHeader == "commenti")
            {
                mapping.NotesColumn = header;
            }
        }

        return mapping;
    }

    private List<CsvImportError> ValidateRow(CsvPreviewRow row, ColumnMapping mapping)
    {
        var errors = new List<CsvImportError>();

        // Validate ProductCode
        if (string.IsNullOrWhiteSpace(mapping.ProductCodeColumn) || 
            !row.Values.TryGetValue(mapping.ProductCodeColumn, out var productCode) ||
            string.IsNullOrWhiteSpace(productCode))
        {
            errors.Add(new CsvImportError
            {
                RowNumber = row.RowNumber,
                ErrorType = "ValidationError",
                ErrorMessage = "ProductCode is required"
            });
        }

        // Validate UnitCost
        if (string.IsNullOrWhiteSpace(mapping.UnitCostColumn) || 
            !row.Values.TryGetValue(mapping.UnitCostColumn, out var unitCostStr) ||
            string.IsNullOrWhiteSpace(unitCostStr))
        {
            errors.Add(new CsvImportError
            {
                RowNumber = row.RowNumber,
                ErrorType = "ValidationError",
                ErrorMessage = "UnitCost is required"
            });
        }
        else if (!decimal.TryParse(unitCostStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitCost) || unitCost <= 0)
        {
            errors.Add(new CsvImportError
            {
                RowNumber = row.RowNumber,
                ErrorType = "ValidationError",
                ErrorMessage = "UnitCost must be a valid number greater than 0"
            });
        }

        // Validate optional numeric fields
        if (!string.IsNullOrWhiteSpace(mapping.LeadTimeDaysColumn) && 
            row.Values.TryGetValue(mapping.LeadTimeDaysColumn, out var leadTimeStr) &&
            !string.IsNullOrWhiteSpace(leadTimeStr))
        {
            if (!int.TryParse(leadTimeStr, out var leadTime) || leadTime < 0)
            {
                errors.Add(new CsvImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorType = "ValidationError",
                    ErrorMessage = "LeadTimeDays must be a valid integer >= 0"
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(mapping.MinOrderQuantityColumn) && 
            row.Values.TryGetValue(mapping.MinOrderQuantityColumn, out var minOrderStr) &&
            !string.IsNullOrWhiteSpace(minOrderStr))
        {
            if (!int.TryParse(minOrderStr, out var minOrder) || minOrder < 1)
            {
                errors.Add(new CsvImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorType = "ValidationError",
                    ErrorMessage = "MinOrderQuantity must be a valid integer >= 1"
                });
            }
        }

        return errors;
    }

    private string? GetFieldValue(CsvHelper.CsvReader csv, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return null;

        try
        {
            return csv.GetField(columnName);
        }
        catch
        {
            return null;
        }
    }
}
