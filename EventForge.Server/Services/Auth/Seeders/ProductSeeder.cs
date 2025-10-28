using Microsoft.EntityFrameworkCore;
using ProductEntity = EventForge.Server.Data.Entities.Products.Product;
using ProductStatusEnum = EventForge.Server.Data.Entities.Products.ProductStatus;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of product seeding service.
/// </summary>
public class ProductSeeder : IProductSeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<ProductSeeder> _logger;

    public ProductSeeder(
        EventForgeDbContext dbContext,
        ILogger<ProductSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> SeedDemoProductsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding demo products for tenant {TenantId}...", tenantId);

            // Check if products already exist for this tenant
            var existingProductCount = await _dbContext.Products
                .CountAsync(p => p.TenantId == tenantId, cancellationToken);

            if (existingProductCount > 0)
            {
                _logger.LogInformation("Tenant {TenantId} already has {Count} products. Skipping demo product seeding.", 
                    tenantId, existingProductCount);
                return true;
            }

            // Get default VAT rate (22%)
            var vatRate22 = await _dbContext.VatRates
                .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Percentage == 22m, cancellationToken);

            // Get IVA 10% rate
            var vatRate10 = await _dbContext.VatRates
                .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Percentage == 10m, cancellationToken);

            // Get IVA 4% rate
            var vatRate4 = await _dbContext.VatRates
                .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Percentage == 4m, cancellationToken);

            // Get default unit of measure (Pezzo)
            var umPezzo = await _dbContext.UMs
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Symbol == "pz", cancellationToken);

            // Get kg unit
            var umKg = await _dbContext.UMs
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Symbol == "kg", cancellationToken);

            // Get litro unit
            var umLitro = await _dbContext.UMs
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Symbol == "l", cancellationToken);

            // Get confezione unit
            var umConfezione = await _dbContext.UMs
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Symbol == "conf", cancellationToken);

            // Create 10 demo products with diverse, realistic Italian data
            var demoProducts = new[]
            {
                new ProductEntity
                {
                    Name = "Laptop Dell XPS 13",
                    ShortDescription = "Notebook ultraleggero professionale",
                    Description = "Laptop Dell XPS 13 con processore Intel Core i7, 16GB RAM, SSD 512GB. Schermo 13.3\" Full HD, Windows 11 Pro. Ideale per professionisti e smart working.",
                    Code = "LAP-DELL-XPS13",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 1299.00m,
                    IsVatIncluded = false,
                    VatRateId = vatRate22?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 5m,
                    SafetyStock = 3m,
                    TargetStockLevel = 15m,
                    AverageDailyDemand = 0.5m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Mouse Wireless Logitech MX Master 3",
                    ShortDescription = "Mouse ergonomico wireless",
                    Description = "Mouse wireless ergonomico Logitech MX Master 3. Sensore ad alta precisione 4000 DPI, 7 pulsanti programmabili, batteria ricaricabile con autonomia fino a 70 giorni. Compatibile con Windows, Mac e Linux.",
                    Code = "MOUSE-LOG-MX3",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 99.90m,
                    IsVatIncluded = false,
                    VatRateId = vatRate22?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 10m,
                    SafetyStock = 5m,
                    TargetStockLevel = 30m,
                    AverageDailyDemand = 2m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Caffè Espresso Arabica 100% - 1Kg",
                    ShortDescription = "Caffè in grani Arabica premium",
                    Description = "Caffè in grani 100% Arabica di alta qualità. Tostatura media, aroma intenso e persistente. Provenienza Sud America. Confezione sottovuoto da 1kg. Ideale per espresso e moka.",
                    Code = "CAFFE-ARA-1KG",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 24.50m,
                    IsVatIncluded = false,
                    VatRateId = vatRate10?.Id,
                    UnitOfMeasureId = umKg?.Id,
                    ReorderPoint = 20m,
                    SafetyStock = 10m,
                    TargetStockLevel = 50m,
                    AverageDailyDemand = 5m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Olio Extra Vergine di Oliva DOP - 1L",
                    ShortDescription = "Olio EVO Toscano IGP",
                    Description = "Olio Extra Vergine di Oliva DOP Toscano. Spremitura a freddo, acidità inferiore a 0.5%. Sapore fruttato con note di mandorla e carciofo. Bottiglia in vetro scuro da 1 litro.",
                    Code = "OLIO-EVO-TOSC-1L",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 15.90m,
                    IsVatIncluded = false,
                    VatRateId = vatRate10?.Id,
                    UnitOfMeasureId = umLitro?.Id,
                    ReorderPoint = 30m,
                    SafetyStock = 15m,
                    TargetStockLevel = 80m,
                    AverageDailyDemand = 8m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Pasta di Semola Integrale Bio - 500g",
                    ShortDescription = "Pasta integrale biologica",
                    Description = "Pasta di semola di grano duro integrale biologica. Formato spaghetti n.5. Certificazione biologica italiana. Tempo di cottura 9-11 minuti. Ricca di fibre. Confezione da 500g.",
                    Code = "PASTA-INT-BIO-500G",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 2.80m,
                    IsVatIncluded = false,
                    VatRateId = vatRate10?.Id,
                    UnitOfMeasureId = umConfezione?.Id,
                    ReorderPoint = 50m,
                    SafetyStock = 25m,
                    TargetStockLevel = 150m,
                    AverageDailyDemand = 15m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Monitor LED 27\" Full HD Samsung",
                    ShortDescription = "Monitor professionale 27 pollici",
                    Description = "Monitor LED 27 pollici Full HD 1920x1080. Tecnologia IPS, tempo di risposta 5ms, frequenza 75Hz. Connessioni HDMI, DisplayPort, VGA. Supporto VESA. Ideale per ufficio e gaming casual.",
                    Code = "MON-SAMS-27FHD",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 189.00m,
                    IsVatIncluded = false,
                    VatRateId = vatRate22?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 8m,
                    SafetyStock = 4m,
                    TargetStockLevel = 20m,
                    AverageDailyDemand = 1.5m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Tastiera Meccanica RGB Gaming",
                    ShortDescription = "Tastiera meccanica retroilluminata",
                    Description = "Tastiera meccanica gaming con switch meccanici blue. Layout italiano QWERTY. Retroilluminazione RGB personalizzabile. Poggiapolsi staccabile. Anti-ghosting completo. Cavo USB removibile.",
                    Code = "TAST-MECH-RGB-IT",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 79.90m,
                    IsVatIncluded = false,
                    VatRateId = vatRate22?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 12m,
                    SafetyStock = 6m,
                    TargetStockLevel = 25m,
                    AverageDailyDemand = 2.5m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Latte Intero Fresco Bio - 1L",
                    ShortDescription = "Latte fresco biologico intero",
                    Description = "Latte fresco pastorizzato intero da agricoltura biologica. 3.5% grassi. Provenienza Italia. Certificazione biologica UE. Da consumarsi entro 5 giorni dall'apertura. Bottiglia in plastica riciclabile da 1 litro.",
                    Code = "LATTE-INTERO-BIO-1L",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 1.80m,
                    IsVatIncluded = false,
                    VatRateId = vatRate4?.Id,
                    UnitOfMeasureId = umLitro?.Id,
                    ReorderPoint = 100m,
                    SafetyStock = 50m,
                    TargetStockLevel = 200m,
                    AverageDailyDemand = 30m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Webcam Full HD 1080p con Microfono",
                    ShortDescription = "Webcam professionale per videoconferenze",
                    Description = "Webcam Full HD 1080p con microfono stereo integrato. Autofocus automatico, correzione della luce, riduzione del rumore. Campo visivo 90°. Compatibile con Zoom, Teams, Skype. Clip universale per monitor.",
                    Code = "WEBCAM-FHD-1080",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 49.90m,
                    IsVatIncluded = false,
                    VatRateId = vatRate22?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 15m,
                    SafetyStock = 8m,
                    TargetStockLevel = 35m,
                    AverageDailyDemand = 3m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    Name = "Pane Integrale Biologico - 500g",
                    ShortDescription = "Pane integrale fresco bio",
                    Description = "Pane integrale biologico con farina di grano duro macinata a pietra. Lievitazione naturale con lievito madre. Senza conservanti. Ricco di fibre. Peso 500g. Prodotto artigianalmente.",
                    Code = "PANE-INT-BIO-500G",
                    Status = ProductStatusEnum.Active,
                    DefaultPrice = 3.20m,
                    IsVatIncluded = false,
                    VatRateId = vatRate4?.Id,
                    UnitOfMeasureId = umPezzo?.Id,
                    ReorderPoint = 40m,
                    SafetyStock = 20m,
                    TargetStockLevel = 100m,
                    AverageDailyDemand = 25m,
                    IsBundle = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Add products to database
            await _dbContext.Products.AddRangeAsync(demoProducts, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Count} demo products for tenant {TenantId}", 
                demoProducts.Length, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo products for tenant {TenantId}", tenantId);
            return false;
        }
    }
}
