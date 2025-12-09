using EventForge.DTOs.Warehouse;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Products;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service for diagnosing and repairing inventory document issues.
/// </summary>
public class InventoryDiagnosticService : IInventoryDiagnosticService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<InventoryDiagnosticService> _logger;
    private readonly IProductService _productService;
    private readonly IStorageLocationService _storageLocationService;

    public InventoryDiagnosticService(
        EventForgeDbContext context,
        ILogger<InventoryDiagnosticService> logger,
        IProductService productService,
        IStorageLocationService storageLocationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
    }

    public async Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting diagnostic for inventory document {DocumentId}", documentId);

        var report = new InventoryDiagnosticReportDto
        {
            DocumentId = documentId,
            AnalyzedAt = DateTime.UtcNow,
            IsHealthy = true
        };

        // 1. Get all document rows
        var rows = await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        report.TotalRows = rows.Count;

        if (rows.Count == 0)
        {
            report.IsHealthy = true;
            return report;
        }

        // 2. Check for missing ProductId or LocationId
        var rowsWithMissingData = rows.Where(r => r.ProductId == null || r.LocationId == null).ToList();
        report.Stats.RowsWithMissingData = rowsWithMissingData.Count;

        foreach (var row in rowsWithMissingData)
        {
            var missingFields = new List<string>();
            if (row.ProductId == null) missingFields.Add("ProductId");
            if (row.LocationId == null) missingFields.Add("LocationId");

            report.Issues.Add(new InventoryDiagnosticIssue
            {
                RowId = row.Id,
                IssueType = "MISSING_DATA",
                Severity = "Error",
                Description = $"Row missing required field(s): {string.Join(", ", missingFields)}",
                CanAutoFix = false
            });
            report.IsHealthy = false;
        }

        // 3. Check for invalid references (non-existent products/locations)
        var productIds = rows.Where(r => r.ProductId != null).Select(r => r.ProductId!.Value).Distinct().ToList();
        var locationIds = rows.Where(r => r.LocationId != null).Select(r => r.LocationId!.Value).Distinct().ToList();

        var existingProductIds = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var existingLocationIds = await _context.StorageLocations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var missingProductIds = productIds.Except(existingProductIds).ToHashSet();
        var missingLocationIds = locationIds.Except(existingLocationIds).ToHashSet();

        foreach (var row in rows.Where(r => r.ProductId.HasValue && missingProductIds.Contains(r.ProductId.Value)))
        {
            report.Issues.Add(new InventoryDiagnosticIssue
            {
                RowId = row.Id,
                IssueType = "INVALID_PRODUCT_REFERENCE",
                Severity = "Error",
                Description = $"Row references non-existent product {row.ProductId}",
                CanAutoFix = true
            });
            report.Stats.InvalidReferences++;
            report.IsHealthy = false;
        }

        foreach (var row in rows.Where(r => r.LocationId.HasValue && missingLocationIds.Contains(r.LocationId.Value)))
        {
            report.Issues.Add(new InventoryDiagnosticIssue
            {
                RowId = row.Id,
                IssueType = "INVALID_LOCATION_REFERENCE",
                Severity = "Error",
                Description = $"Row references non-existent location {row.LocationId}",
                CanAutoFix = true
            });
            report.Stats.InvalidReferences++;
            report.IsHealthy = false;
        }

        // 4. Check for duplicates (same ProductId + LocationId)
        var duplicateGroups = rows
            .Where(r => r.ProductId != null && r.LocationId != null)
            .GroupBy(r => new { r.ProductId, r.LocationId })
            .Where(g => g.Count() > 1)
            .ToList();

        report.Stats.DuplicateProducts = duplicateGroups.Sum(g => g.Count() - 1);

        foreach (var group in duplicateGroups)
        {
            foreach (var row in group.Skip(1))
            {
                report.Issues.Add(new InventoryDiagnosticIssue
                {
                    RowId = row.Id,
                    IssueType = "DUPLICATE_ENTRY",
                    Severity = "Warning",
                    Description = $"Duplicate entry for Product {row.ProductId} at Location {row.LocationId}",
                    CanAutoFix = true
                });
            }
            if (report.Stats.DuplicateProducts > 0)
            {
                report.IsHealthy = false;
            }
        }

        // 5. Check for negative quantities
        var rowsWithNegativeQty = rows.Where(r => r.Quantity < 0).ToList();
        report.Stats.NegativeQuantities = rowsWithNegativeQty.Count;

        foreach (var row in rowsWithNegativeQty)
        {
            report.Issues.Add(new InventoryDiagnosticIssue
            {
                RowId = row.Id,
                IssueType = "NEGATIVE_QUANTITY",
                Severity = "Warning",
                Description = $"Row has negative quantity: {row.Quantity}",
                CanAutoFix = true
            });
            report.IsHealthy = false;
        }

        report.TotalIssues = report.Issues.Count;

        _logger.LogInformation(
            "Diagnostic completed for document {DocumentId}. Total rows: {TotalRows}, Issues: {TotalIssues}, Healthy: {IsHealthy}",
            documentId, report.TotalRows, report.TotalIssues, report.IsHealthy);

        return report;
    }

    public async Task<InventoryRepairResultDto> AutoRepairDocumentAsync(
        Guid documentId,
        InventoryAutoRepairOptionsDto options,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting auto-repair for inventory document {DocumentId}", documentId);

        var result = new InventoryRepairResultDto
        {
            DocumentId = documentId,
            RepairedAt = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get all rows
            var rows = await _context.DocumentRows
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            result.RowsAnalyzed = rows.Count;

            // 1. Remove rows with invalid references if enabled
            if (options.RemoveInvalidReferences)
            {
                var productIds = rows.Where(r => r.ProductId != null).Select(r => r.ProductId!.Value).Distinct().ToList();
                var locationIds = rows.Where(r => r.LocationId != null).Select(r => r.LocationId!.Value).Distinct().ToList();

                var existingProductIds = (await _context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken)).ToHashSet();

                var existingLocationIds = (await _context.StorageLocations
                    .AsNoTracking()
                    .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
                    .Select(l => l.Id)
                    .ToListAsync(cancellationToken)).ToHashSet();

                var rowsToRemove = rows.Where(r =>
                    (r.ProductId.HasValue && !existingProductIds.Contains(r.ProductId.Value)) ||
                    (r.LocationId.HasValue && !existingLocationIds.Contains(r.LocationId.Value))
                ).ToList();

                foreach (var row in rowsToRemove)
                {
                    row.IsDeleted = true;
                    row.ModifiedBy = currentUser;
                    row.ModifiedAt = DateTime.UtcNow;
                }

                result.RowsRemoved += rowsToRemove.Count;
                if (rowsToRemove.Count > 0)
                {
                    result.ActionsPerformed.Add($"Removed {rowsToRemove.Count} row(s) with invalid references");
                }
            }

            // 2. Merge duplicates if enabled
            if (options.MergeDuplicates)
            {
                var validRows = rows.Where(r => !r.IsDeleted && r.ProductId != null && r.LocationId != null).ToList();
                var duplicateGroups = validRows
                    .GroupBy(r => new { r.ProductId, r.LocationId })
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in duplicateGroups)
                {
                    var firstRow = group.First();
                    var duplicates = group.Skip(1).ToList();

                    // Merge quantities
                    foreach (var dup in duplicates)
                    {
                        firstRow.Quantity += dup.Quantity;
                        dup.IsDeleted = true;
                        dup.ModifiedBy = currentUser;
                        dup.ModifiedAt = DateTime.UtcNow;
                    }

                    firstRow.ModifiedBy = currentUser;
                    firstRow.ModifiedAt = DateTime.UtcNow;

                    result.RowsCorrected += duplicates.Count;
                }

                if (duplicateGroups.Count > 0)
                {
                    result.ActionsPerformed.Add($"Merged {duplicateGroups.Sum(g => g.Count() - 1)} duplicate row(s)");
                }
            }

            // 3. Convert negative quantities if enabled
            if (options.ConvertNegativeQuantities)
            {
                var negativeRows = rows.Where(r => !r.IsDeleted && r.Quantity < 0).ToList();

                foreach (var row in negativeRows)
                {
                    row.Quantity = Math.Abs(row.Quantity);
                    row.ModifiedBy = currentUser;
                    row.ModifiedAt = DateTime.UtcNow;
                }

                result.RowsCorrected += negativeRows.Count;
                if (negativeRows.Count > 0)
                {
                    result.ActionsPerformed.Add($"Converted {negativeRows.Count} negative quantit(ies) to positive");
                }
            }

            // 4. Fix missing location data if enabled and default location provided
            if (options.FixMissingData && options.DefaultLocationId.HasValue)
            {
                var rowsWithMissingLocation = rows.Where(r => !r.IsDeleted && r.LocationId == null && r.ProductId != null).ToList();

                foreach (var row in rowsWithMissingLocation)
                {
                    row.LocationId = options.DefaultLocationId.Value;
                    row.ModifiedBy = currentUser;
                    row.ModifiedAt = DateTime.UtcNow;
                }

                result.RowsCorrected += rowsWithMissingLocation.Count;
                if (rowsWithMissingLocation.Count > 0)
                {
                    result.ActionsPerformed.Add($"Assigned default location to {rowsWithMissingLocation.Count} row(s)");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-repair completed for document {DocumentId}. Analyzed: {RowsAnalyzed}, Corrected: {RowsCorrected}, Removed: {RowsRemoved}",
                documentId, result.RowsAnalyzed, result.RowsCorrected, result.RowsRemoved);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error during auto-repair for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<bool> RepairRowAsync(
        Guid documentId,
        Guid rowId,
        InventoryRowRepairDto repairData,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Repairing row {RowId} in document {DocumentId}", rowId, documentId);

        var row = await _context.DocumentRows
            .Where(r => r.Id == rowId && r.DocumentHeaderId == documentId && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
        {
            _logger.LogWarning("Row {RowId} not found in document {DocumentId}", rowId, documentId);
            return false;
        }

        var modified = false;

        if (repairData.NewProductId.HasValue && repairData.NewProductId.Value != row.ProductId)
        {
            row.ProductId = repairData.NewProductId.Value;
            modified = true;
        }

        if (repairData.NewLocationId.HasValue && repairData.NewLocationId.Value != row.LocationId)
        {
            row.LocationId = repairData.NewLocationId.Value;
            modified = true;
        }

        if (repairData.NewQuantity.HasValue && repairData.NewQuantity.Value != row.Quantity)
        {
            row.Quantity = repairData.NewQuantity.Value;
            modified = true;
        }

        if (repairData.NewNotes != null && repairData.NewNotes != row.Notes)
        {
            row.Notes = repairData.NewNotes;
            modified = true;
        }

        if (modified)
        {
            row.ModifiedBy = currentUser;
            row.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Row {RowId} repaired successfully", rowId);
        }

        return modified;
    }

    public async Task<int> RemoveProblematicRowsAsync(
        Guid documentId,
        List<Guid> rowIds,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing {Count} problematic rows from document {DocumentId}", rowIds.Count, documentId);

        var rows = await _context.DocumentRows
            .Where(r => rowIds.Contains(r.Id) && r.DocumentHeaderId == documentId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.ModifiedBy = currentUser;
            row.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed {Count} rows from document {DocumentId}", rows.Count, documentId);

        return rows.Count;
    }
}
