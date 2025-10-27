using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of entity seeding service for tenant base entities.
/// </summary>
public class EntitySeeder : IEntitySeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<EntitySeeder> _logger;

    public EntitySeeder(
        EventForgeDbContext dbContext,
        ILogger<EntitySeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> SeedTenantBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding base entities for tenant {TenantId}...", tenantId);

            // Seed VAT natures first (needed for VAT rates)
            if (!await SeedVatNaturesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed VAT natures");
                return false;
            }

            // Seed VAT rates
            if (!await SeedVatRatesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed VAT rates");
                return false;
            }

            // Seed units of measure
            if (!await SeedUnitsOfMeasureAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed units of measure");
                return false;
            }

            // Seed default warehouse and storage location
            if (!await SeedDefaultWarehouseAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed default warehouse");
                return false;
            }

            // Seed document types
            if (!await SeedDocumentTypesAsync(tenantId, cancellationToken))
            {
                _logger.LogError("Failed to seed document types");
                return false;
            }

            _logger.LogInformation("Base entities seeded successfully for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding base entities for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds Italian VAT nature codes (Natura IVA).
    /// </summary>
    private async Task<bool> SeedVatNaturesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT natures for tenant {TenantId}...", tenantId);

            // Check if VAT natures already exist for this tenant
            var existingNatures = await _dbContext.VatNatures
                .AnyAsync(v => v.TenantId == tenantId, cancellationToken);

            if (existingNatures)
            {
                _logger.LogInformation("VAT natures already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Italian VAT nature codes as per current legislation
            var vatNatures = new[]
            {
                new VatNature
                {
                    Code = "N1",
                    Name = "Escluse ex art. 15",
                    Description = "Operazioni escluse dal campo di applicazione dell'IVA ex art. 15 del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2",
                    Name = "Non soggette",
                    Description = "Operazioni non soggette ad IVA ai sensi degli artt. da 7 a 7-septies del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2.1",
                    Name = "Non soggette ad IVA ai sensi degli artt. da 7 a 7-septies",
                    Description = "Operazioni non soggette - Cessioni di beni e prestazioni di servizi non soggette per carenza del presupposto territoriale",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N2.2",
                    Name = "Non soggette ad IVA - Altre operazioni",
                    Description = "Operazioni non soggette - Altre operazioni che non configurano una cessione di beni né una prestazione di servizi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3",
                    Name = "Non imponibili",
                    Description = "Operazioni non imponibili (esportazioni, cessioni intracomunitarie, ecc.)",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.1",
                    Name = "Non imponibili - Esportazioni",
                    Description = "Esportazioni di cui agli artt. 8 e 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.2",
                    Name = "Non imponibili - Cessioni intracomunitarie",
                    Description = "Cessioni intracomunitarie di cui all'art. 41 del DL 331/93",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.3",
                    Name = "Non imponibili - Cessioni verso San Marino",
                    Description = "Cessioni verso San Marino di cui all'art. 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.4",
                    Name = "Non imponibili - Operazioni assimilate",
                    Description = "Operazioni assimilate alle cessioni all'esportazione di cui all'art. 8-bis del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.5",
                    Name = "Non imponibili - Altre operazioni",
                    Description = "Operazioni non imponibili a seguito di dichiarazioni d'intento",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N3.6",
                    Name = "Non imponibili - Altre operazioni non imponibili",
                    Description = "Altre operazioni non imponibili che non concorrono alla formazione del plafond",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N4",
                    Name = "Esenti",
                    Description = "Operazioni esenti da IVA ai sensi degli artt. 10 del DPR 633/72",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N5",
                    Name = "Regime del margine",
                    Description = "Regime del margine / IVA non esposta in fattura ai sensi dell'art. 36 del DL 41/95",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6",
                    Name = "Inversione contabile",
                    Description = "Inversione contabile (reverse charge) per cessioni di rottami, altri materiali, subappalti, ecc.",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.1",
                    Name = "Inversione contabile - Cessioni di rottami",
                    Description = "Inversione contabile - Cessione di rottami e altri materiali di recupero",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.2",
                    Name = "Inversione contabile - Cessioni di oro e argento",
                    Description = "Inversione contabile - Cessione di oro e argento puro",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.3",
                    Name = "Inversione contabile - Subappalto",
                    Description = "Inversione contabile - Subappalto nel settore edile",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.4",
                    Name = "Inversione contabile - Cessioni di fabbricati",
                    Description = "Inversione contabile - Cessioni di fabbricati",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.5",
                    Name = "Inversione contabile - Cessioni di telefoni cellulari",
                    Description = "Inversione contabile - Cessioni di telefoni cellulari",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.6",
                    Name = "Inversione contabile - Cessioni di prodotti elettronici",
                    Description = "Inversione contabile - Cessioni di prodotti elettronici",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.7",
                    Name = "Inversione contabile - Prestazioni settore edile",
                    Description = "Inversione contabile - Prestazioni comparto edile e settori connessi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.8",
                    Name = "Inversione contabile - Operazioni settore energetico",
                    Description = "Inversione contabile - Operazioni settore energetico",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N6.9",
                    Name = "Inversione contabile - Altri casi",
                    Description = "Inversione contabile - Altri casi",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatNature
                {
                    Code = "N7",
                    Name = "IVA assolta in altro stato UE",
                    Description = "IVA assolta in altro stato UE (vendite a distanza ex art. 40 c. 3 e 4 e art. 41 c. 1 lett. b, DL 331/93)",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.VatNatures.AddRange(vatNatures);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} VAT natures for tenant {TenantId}", vatNatures.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding VAT natures for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds Italian VAT rates (current legislation).
    /// </summary>
    private async Task<bool> SeedVatRatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT rates for tenant {TenantId}...", tenantId);

            // Check if VAT rates already exist for this tenant
            var existingRates = await _dbContext.VatRates
                .AnyAsync(v => v.TenantId == tenantId, cancellationToken);

            if (existingRates)
            {
                _logger.LogInformation("VAT rates already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Italian VAT rates as per current legislation (2024-2025)
            var vatRates = new[]
            {
                new VatRate
                {
                    Name = "IVA 22%",
                    Percentage = 22m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ordinaria",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 10%",
                    Percentage = 10m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ridotta - Generi alimentari, bevande, servizi turistici",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 5%",
                    Percentage = 5m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA ridotta - Generi di prima necessità",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 4%",
                    Percentage = 4m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Aliquota IVA minima - Generi di prima necessità (pane, latte, ecc.)",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new VatRate
                {
                    Name = "IVA 0%",
                    Percentage = 0m,
                    Status = Data.Entities.Common.ProductVatRateStatus.Active,
                    ValidFrom = DateTime.UtcNow,
                    Notes = "Operazioni non imponibili, esenti o fuori campo IVA",
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.VatRates.AddRange(vatRates);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} VAT rates for tenant {TenantId}", vatRates.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding VAT rates for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds common units of measure for warehouse management.
    /// </summary>
    private async Task<bool> SeedUnitsOfMeasureAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding units of measure for tenant {TenantId}...", tenantId);

            // Check if units of measure already exist for this tenant
            var existingUnits = await _dbContext.UMs
                .AnyAsync(u => u.TenantId == tenantId, cancellationToken);

            if (existingUnits)
            {
                _logger.LogInformation("Units of measure already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Common units of measure for warehouse management
            var unitsOfMeasure = new[]
            {
                // Count/Piece units
                new UM
                {
                    Name = "Pezzo",
                    Symbol = "pz",
                    Description = "Unità di misura per pezzi singoli",
                    IsDefault = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Confezione",
                    Symbol = "conf",
                    Description = "Unità di misura per confezioni",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Scatola",
                    Symbol = "scat",
                    Description = "Unità di misura per scatole",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Cartone",
                    Symbol = "cart",
                    Description = "Unità di misura per cartoni",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Pallet",
                    Symbol = "pallet",
                    Description = "Unità di misura per pallet",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Bancale",
                    Symbol = "banc",
                    Description = "Unità di misura per bancali",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Collo",
                    Symbol = "collo",
                    Description = "Unità di misura per colli",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Weight units
                new UM
                {
                    Name = "Kilogrammo",
                    Symbol = "kg",
                    Description = "Unità di misura di peso - chilogrammi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Grammo",
                    Symbol = "g",
                    Description = "Unità di misura di peso - grammi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Tonnellata",
                    Symbol = "t",
                    Description = "Unità di misura di peso - tonnellate",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Quintale",
                    Symbol = "q",
                    Description = "Unità di misura di peso - quintali",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Volume units
                new UM
                {
                    Name = "Litro",
                    Symbol = "l",
                    Description = "Unità di misura di volume - litri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Millilitro",
                    Symbol = "ml",
                    Description = "Unità di misura di volume - millilitri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Metro cubo",
                    Symbol = "m³",
                    Description = "Unità di misura di volume - metri cubi",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Length units
                new UM
                {
                    Name = "Metro",
                    Symbol = "m",
                    Description = "Unità di misura di lunghezza - metri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Centimetro",
                    Symbol = "cm",
                    Description = "Unità di misura di lunghezza - centimetri",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Metro quadrato",
                    Symbol = "m²",
                    Description = "Unità di misura di superficie - metri quadrati",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Other units
                new UM
                {
                    Name = "Paio",
                    Symbol = "paio",
                    Description = "Unità di misura per paia",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Set",
                    Symbol = "set",
                    Description = "Unità di misura per set",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new UM
                {
                    Name = "Kit",
                    Symbol = "kit",
                    Description = "Unità di misura per kit",
                    IsDefault = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.UMs.AddRange(unitsOfMeasure);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} units of measure for tenant {TenantId}", unitsOfMeasure.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding units of measure for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds a default warehouse and storage location for the tenant.
    /// </summary>
    private async Task<bool> SeedDefaultWarehouseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding default warehouse for tenant {TenantId}...", tenantId);

            // Check if warehouses already exist for this tenant
            var existingWarehouses = await _dbContext.StorageFacilities
                .AnyAsync(w => w.TenantId == tenantId, cancellationToken);

            if (existingWarehouses)
            {
                _logger.LogInformation("Warehouses already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Create default warehouse
            var defaultWarehouse = new StorageFacility
            {
                Name = "Magazzino Principale",
                Code = "MAG-01",
                Address = "Indirizzo da completare",
                Notes = "Magazzino principale creato durante l'inizializzazione del sistema",
                IsFiscal = true,
                IsActive = true,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _dbContext.StorageFacilities.Add(defaultWarehouse);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            // Create default storage location
            var defaultLocation = new StorageLocation
            {
                Code = "UB-DEF",
                Description = "Ubicazione predefinita",
                WarehouseId = defaultWarehouse.Id,
                IsActive = true,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _dbContext.StorageLocations.Add(defaultLocation);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created default warehouse '{WarehouseName}' with default location '{LocationCode}' for tenant {TenantId}",
                defaultWarehouse.Name, defaultLocation.Code, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default warehouse for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds standard document types for the tenant.
    /// </summary>
    private async Task<bool> SeedDocumentTypesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding document types for tenant {TenantId}...", tenantId);

            // Check if document types already exist for this tenant
            var existingDocTypes = await _dbContext.DocumentTypes
                .AnyAsync(dt => dt.TenantId == tenantId, cancellationToken);

            if (existingDocTypes)
            {
                _logger.LogInformation("Document types already exist for tenant {TenantId}", tenantId);
                return true;
            }

            // Get the default warehouse for document type configuration
            var defaultWarehouse = await _dbContext.StorageFacilities
                .FirstOrDefaultAsync(w => w.TenantId == tenantId, cancellationToken);

            // Standard document types for Italian businesses
            var documentTypes = new[]
            {
                // Inventory Document
                new DocumentType
                {
                    Code = "INVENTORY",
                    Name = "Documento di Inventario",
                    Notes = "Documento per la rilevazione fisica dell'inventario di magazzino",
                    IsStockIncrease = false, // Inventory adjustments can go either way
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Delivery Note
                new DocumentType
                {
                    Code = "DDT_VEND",
                    Name = "Bolla di Vendita (DDT)",
                    Notes = "Documento di trasporto per vendita - riduce giacenza magazzino",
                    IsStockIncrease = false, // Delivery decreases stock
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Delivery Note
                new DocumentType
                {
                    Code = "DDT_ACQ",
                    Name = "Bolla di Acquisto (DDT)",
                    Notes = "Documento di trasporto per acquisto - aumenta giacenza magazzino",
                    IsStockIncrease = true, // Purchase increases stock
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Transfer Note
                new DocumentType
                {
                    Code = "DDT_TRASF",
                    Name = "Bolla di Trasferimento",
                    Notes = "Documento di trasporto per trasferimento tra magazzini",
                    IsStockIncrease = false, // Transfer is neutral (reduces source, increases destination)
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Invoice
                new DocumentType
                {
                    Code = "FATT_VEND",
                    Name = "Fattura di Vendita",
                    Notes = "Fattura di vendita - riduce giacenza magazzino se non già movimentata",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Invoice
                new DocumentType
                {
                    Code = "FATT_ACQ",
                    Name = "Fattura di Acquisto",
                    Notes = "Fattura di acquisto - aumenta giacenza magazzino se non già movimentata",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Receipt
                new DocumentType
                {
                    Code = "SCONTRINO",
                    Name = "Scontrino di Vendita",
                    Notes = "Scontrino fiscale per vendita al dettaglio - riduce giacenza magazzino",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Sales Order
                new DocumentType
                {
                    Code = "ORD_VEND",
                    Name = "Ordine di Vendita",
                    Notes = "Ordine cliente - non movimenta giacenza fino all'evasione",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Purchase Order
                new DocumentType
                {
                    Code = "ORD_ACQ",
                    Name = "Ordine di Acquisto",
                    Notes = "Ordine fornitore - non movimenta giacenza fino al ricevimento",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Quote
                new DocumentType
                {
                    Code = "PREVENTIVO",
                    Name = "Preventivo",
                    Notes = "Preventivo/offerta cliente - non movimenta giacenza",
                    IsStockIncrease = false,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = false,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Return Note
                new DocumentType
                {
                    Code = "RESO",
                    Name = "Reso da Cliente",
                    Notes = "Documento per resi da cliente - aumenta giacenza magazzino",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                // Credit Note
                new DocumentType
                {
                    Code = "NOTA_CRED",
                    Name = "Nota di Credito",
                    Notes = "Nota di credito - può aumentare giacenza in caso di reso",
                    IsStockIncrease = true,
                    DefaultWarehouseId = defaultWarehouse?.Id,
                    IsFiscal = true,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.DocumentTypes.AddRange(documentTypes);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} document types for tenant {TenantId}", documentTypes.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding document types for tenant {TenantId}", tenantId);
            return false;
        }
    }
}
