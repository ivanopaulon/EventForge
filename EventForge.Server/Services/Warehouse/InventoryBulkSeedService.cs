using EventForge.DTOs.Documents;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Documents;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for bulk seeding inventory documents with all active tenant products.
/// </summary>
public class InventoryBulkSeedService : IInventoryBulkSeedService
{
    private readonly EventForgeDbContext _context;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InventoryBulkSeedService> _logger;
    
    // Thread-local Random to avoid thread safety issues
    private static readonly ThreadLocal<Random> _random = new(() => new Random());

    public InventoryBulkSeedService(
        EventForgeDbContext context,
        IDocumentHeaderService documentHeaderService,
        IStorageLocationService storageLocationService,
        ITenantContext tenantContext,
        ILogger<InventoryBulkSeedService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InventorySeedResultDto> SeedInventoryAsync(
        InventorySeedRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new InventorySeedResultDto();

        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            _logger.LogInformation(
                "Starting inventory seed for tenant {TenantId} - Mode: {Mode}, CreateDocument: {CreateDocument}, BatchSize: {BatchSize}",
                currentTenantId.Value, request.Mode, request.CreateDocument, request.BatchSize);

            // Validate request
            ValidateRequest(request);

            // Get all active products for the tenant
            var products = await _context.Products
                .Where(p => p.TenantId == currentTenantId.Value && p.Status == Data.Entities.Products.ProductStatus.Active && !p.IsDeleted)
                .OrderBy(p => p.Code)
                .ToListAsync(cancellationToken);

            result.ProductsFound = products.Count;

            if (products.Count == 0)
            {
                result.Message = "Nessun prodotto attivo trovato per questo tenant.";
                _logger.LogWarning("No active products found for tenant {TenantId}", currentTenantId.Value);
                return result;
            }

            _logger.LogInformation("Found {Count} active products for seeding", products.Count);

            // Get or select location
            Guid locationId;
            if (request.LocationId.HasValue)
            {
                locationId = request.LocationId.Value;
                _logger.LogInformation("Using specified location: {LocationId}", locationId);
            }
            else
            {
                // Get the first available location for this tenant
                var firstLocation = await _context.StorageLocations
                    .Where(sl => sl.TenantId == currentTenantId.Value && !sl.IsDeleted)
                    .OrderBy(sl => sl.Code)
                    .FirstOrDefaultAsync(cancellationToken);

                if (firstLocation == null)
                {
                    throw new InvalidOperationException("Nessuna ubicazione trovata per questo tenant. Creare almeno un'ubicazione prima di seminare l'inventario.");
                }

                locationId = firstLocation.Id;
                _logger.LogInformation("Using default location: {LocationId} ({Code})", locationId, firstLocation.Code);
            }

            // Create inventory document if requested
            Guid? documentId = null;
            if (request.CreateDocument)
            {
                documentId = await CreateInventoryDocumentAsync(
                    currentTenantId.Value,
                    request.DocumentName ?? $"Inventario Seed - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    locationId,
                    currentUser,
                    cancellationToken);

                result.DocumentId = documentId;
                _logger.LogInformation("Created inventory document: {DocumentId}", documentId);
            }

            // Process products in batches
            int rowsCreated = 0;
            int batchSize = Math.Max(1, Math.Min(request.BatchSize, 1000)); // Ensure between 1 and 1000

            for (int i = 0; i < products.Count; i += batchSize)
            {
                var batch = products.Skip(i).Take(batchSize).ToList();
                _logger.LogInformation("Processing batch {BatchNumber} of {TotalBatches} ({Count} products)",
                    (i / batchSize) + 1, (products.Count + batchSize - 1) / batchSize, batch.Count);

                foreach (var product in batch)
                {
                    try
                    {
                        var quantity = CalculateQuantity(request, product);

                        if (documentId.HasValue)
                        {
                            // Add row to document
                            await AddDocumentRowAsync(
                                documentId.Value,
                                product,
                                locationId,
                                quantity,
                                currentUser,
                                cancellationToken);
                        }

                        rowsCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing product {ProductId} ({ProductCode})",
                            product.Id, product.Code);
                        // Continue with next product
                    }
                }

                // Save changes for this batch
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved batch {BatchNumber} - Total rows created: {RowsCreated}",
                    (i / batchSize) + 1, rowsCreated);
            }

            result.RowsCreated = rowsCreated;
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            result.Message = $"Operazione completata con successo. Creati {rowsCreated} righe per {result.ProductsFound} prodotti in {sw.ElapsedMilliseconds}ms.";

            _logger.LogInformation(
                "Inventory seed completed - Products: {ProductsFound}, Rows: {RowsCreated}, Duration: {Duration}ms",
                result.ProductsFound, result.RowsCreated, result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            result.Message = $"Errore durante l'operazione: {ex.Message}";
            _logger.LogError(ex, "Error during inventory seed operation");
            throw;
        }
    }

    private void ValidateRequest(InventorySeedRequestDto request)
    {
        var validModes = new[] { "fixed", "random", "fromProduct" };
        if (!validModes.Contains(request.Mode.ToLowerInvariant()))
        {
            throw new ArgumentException($"Mode non valida. Valori ammessi: {string.Join(", ", validModes)}");
        }

        if (request.Mode.Equals("fixed", StringComparison.OrdinalIgnoreCase) && !request.Quantity.HasValue)
        {
            throw new ArgumentException("Quantity è richiesta quando Mode è 'fixed'.");
        }

        if (request.Mode.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.MinQuantity.HasValue || !request.MaxQuantity.HasValue)
            {
                throw new ArgumentException("MinQuantity e MaxQuantity sono richiesti quando Mode è 'random'.");
            }

            if (request.MinQuantity.Value > request.MaxQuantity.Value)
            {
                throw new ArgumentException("MinQuantity non può essere maggiore di MaxQuantity.");
            }
        }

        if (request.BatchSize < 1 || request.BatchSize > 1000)
        {
            throw new ArgumentException("BatchSize deve essere compreso tra 1 e 1000.");
        }
    }

    private decimal CalculateQuantity(InventorySeedRequestDto request, Product product)
    {
        return request.Mode.ToLowerInvariant() switch
        {
            "fixed" => request.Quantity ?? 0m,
            "random" => GenerateRandomQuantity(request.MinQuantity ?? 0m, request.MaxQuantity ?? 100m),
            "fromproduct" => GetQuantityFromProduct(product, request.Quantity ?? 10m),
            _ => throw new ArgumentException($"Mode non supportata: {request.Mode}")
        };
    }

    private decimal GenerateRandomQuantity(decimal min, decimal max)
    {
        // Generate a random decimal between min and max
        var range = max - min;
        var randomValue = (decimal)_random.Value!.NextDouble() * range;
        return Math.Round(min + randomValue, 2);
    }

    private static string GenerateInventorySeedDocumentNumber()
    {
        // Use consistent format: INV-SEED-YYYYMMDD-HHmmss
        return $"INV-SEED-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    private decimal GetQuantityFromProduct(Product product, decimal fallback)
    {
        // Priority: TargetStockLevel > ReorderPoint > SafetyStock > fallback
        if (product.TargetStockLevel.HasValue && product.TargetStockLevel.Value > 0)
        {
            return product.TargetStockLevel.Value;
        }

        if (product.ReorderPoint.HasValue && product.ReorderPoint.Value > 0)
        {
            return product.ReorderPoint.Value;
        }

        if (product.SafetyStock.HasValue && product.SafetyStock.Value > 0)
        {
            return product.SafetyStock.Value;
        }

        return fallback;
    }

    private async Task<Guid> CreateInventoryDocumentAsync(
        Guid tenantId,
        string documentName,
        Guid warehouseId,
        string currentUser,
        CancellationToken cancellationToken)
    {
        // Get or create inventory document type
        var inventoryDocumentType = await _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(
            tenantId,
            cancellationToken);

        // Get or create system business party for internal operations
        var systemBusinessPartyId = await _documentHeaderService.GetOrCreateSystemBusinessPartyAsync(
            tenantId,
            cancellationToken);

        // Generate document number with seed prefix for easy identification
        var documentNumber = GenerateInventorySeedDocumentNumber();

        // Create document header
        // Note: IsProforma is set to true because this is a test/seed document
        // that doesn't represent actual physical inventory operations
        var createHeaderDto = new CreateDocumentHeaderDto
        {
            DocumentTypeId = inventoryDocumentType.Id,
            Series = "SEED",
            Number = documentNumber,
            Date = DateTime.UtcNow,
            BusinessPartyId = systemBusinessPartyId,
            SourceWarehouseId = warehouseId,
            Notes = documentName,
            IsFiscal = false,
            IsProforma = true // Test document, not a real inventory operation
        };

        var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(
            createHeaderDto,
            currentUser,
            cancellationToken);

        return documentHeader.Id;
    }

    private async Task AddDocumentRowAsync(
        Guid documentId,
        Product product,
        Guid locationId,
        decimal quantity,
        string currentUser,
        CancellationToken cancellationToken)
    {
        // Get unit of measure symbol if available
        string? unitOfMeasure = null;
        if (product.UnitOfMeasureId.HasValue)
        {
            var um = await _context.UMs
                .FirstOrDefaultAsync(u => u.Id == product.UnitOfMeasureId.Value && !u.IsDeleted, cancellationToken);
            unitOfMeasure = um?.Symbol;
        }

        // Get VAT rate if available
        decimal vatRate = 0m;
        string? vatDescription = null;
        if (product.VatRateId.HasValue)
        {
            var vat = await _context.VatRates
                .FirstOrDefaultAsync(v => v.Id == product.VatRateId.Value && !v.IsDeleted, cancellationToken);
            if (vat != null)
            {
                vatRate = vat.Percentage;
                vatDescription = $"VAT {vat.Percentage}%";
            }
        }

        // Get location info
        var location = await _context.StorageLocations
            .FirstOrDefaultAsync(sl => sl.Id == locationId && !sl.IsDeleted, cancellationToken);

        // Create document row
        var createRowDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = documentId,
            ProductCode = product.Code,
            ProductId = product.Id,
            LocationId = locationId,
            Description = product.Name,
            UnitOfMeasure = unitOfMeasure,
            UnitOfMeasureId = product.UnitOfMeasureId,
            Quantity = quantity,
            UnitPrice = 0m,
            VatRate = vatRate,
            VatDescription = vatDescription,
            SourceWarehouseId = location?.WarehouseId,
            Notes = "Riga generata automaticamente dal seed"
        };

        await _documentHeaderService.AddDocumentRowAsync(createRowDto, currentUser, cancellationToken);
    }
}
