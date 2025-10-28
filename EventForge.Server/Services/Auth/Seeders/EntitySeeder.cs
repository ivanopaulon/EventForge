using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of entity seeding service for tenant base entities.
/// </summary>
public class EntitySeeder : IEntitySeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<EntitySeeder> _logger;
    private readonly IProductSeeder _productSeeder;

    public EntitySeeder(
        EventForgeDbContext dbContext,
        ILogger<EntitySeeder> logger,
        IProductSeeder productSeeder)
    {
        _dbContext = dbContext;
        _logger = logger;
        _productSeeder = productSeeder;
    }

    public async Task<bool> SeedTenantBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding base entities for tenant {TenantId}...", tenantId);

            // Validate tenantId
            if (tenantId == Guid.Empty)
            {
                _logger.LogError("Cannot seed base entities for empty tenant ID");
                return false;
            }

            // Verify tenant exists
            var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
            if (!tenantExists)
            {
                _logger.LogError("Tenant {TenantId} does not exist. Cannot seed base entities.", tenantId);
                return false;
            }

            // Use transaction only if not using InMemory database (InMemory doesn't support transactions)
            var isInMemory = _dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false;
            var transaction = isInMemory ? null : await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Seed VAT natures first (needed for VAT rates)
                if (!await SeedVatNaturesAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed VAT natures");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Seed VAT rates
                if (!await SeedVatRatesAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed VAT rates");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Seed units of measure
                if (!await SeedUnitsOfMeasureAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed units of measure");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Seed default warehouse and storage location
                if (!await SeedDefaultWarehouseAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed default warehouse");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Seed document types
                if (!await SeedDocumentTypesAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed document types");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Seed demo products
                if (!await _productSeeder.SeedDemoProductsAsync(tenantId, cancellationToken))
                {
                    _logger.LogError("Failed to seed demo products");
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                // Commit transaction if exists
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation("Base entities seeded successfully for tenant {TenantId}", tenantId);
                return true;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding base entities for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Seeds Italian VAT nature codes (Natura IVA).
    /// Ensures all required codes exist, adding only missing ones.
    /// </summary>
    private async Task<bool> SeedVatNaturesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT natures for tenant {TenantId}...", tenantId);

            // Get existing VAT nature codes for this tenant
            var existingCodes = await _dbContext.VatNatures
                .Where(v => v.TenantId == tenantId)
                .Select(v => v.Code)
                .ToListAsync(cancellationToken);

            var existingCodesSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingCodesSet.Count > 0)
            {
                _logger.LogInformation("Found {Count} existing VAT natures for tenant {TenantId}", existingCodesSet.Count, tenantId);
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

            // Filter to only add missing VAT natures
            var naturesToAdd = vatNatures.Where(vn => !existingCodesSet.Contains(vn.Code)).ToList();

            if (naturesToAdd.Any())
            {
                _dbContext.VatNatures.AddRange(naturesToAdd);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Added {Count} new VAT natures for tenant {TenantId}", naturesToAdd.Count, tenantId);
            }
            else
            {
                _logger.LogInformation("All VAT natures already exist for tenant {TenantId}", tenantId);
            }

            // Verify all expected natures are present
            var finalCount = await _dbContext.VatNatures.CountAsync(v => v.TenantId == tenantId, cancellationToken);
            if (finalCount < 24)
            {
                _logger.LogWarning("Expected 24 VAT natures but found {Count} for tenant {TenantId}", finalCount, tenantId);
            }

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
    /// Ensures all required rates exist, adding only missing ones.
    /// </summary>
    private async Task<bool> SeedVatRatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding VAT rates for tenant {TenantId}...", tenantId);

            // Get existing VAT rate percentages for this tenant
            var existingPercentages = await _dbContext.VatRates
                .Where(v => v.TenantId == tenantId)
                .Select(v => v.Percentage)
                .ToListAsync(cancellationToken);

            var existingPercentagesSet = existingPercentages.ToHashSet();

            if (existingPercentagesSet.Count > 0)
            {
                _logger.LogInformation("Found {Count} existing VAT rates for tenant {TenantId}", existingPercentagesSet.Count, tenantId);
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

            // Filter to only add missing VAT rates
            var ratesToAdd = vatRates.Where(vr => !existingPercentagesSet.Contains(vr.Percentage)).ToList();

            if (ratesToAdd.Any())
            {
                _dbContext.VatRates.AddRange(ratesToAdd);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Added {Count} new VAT rates for tenant {TenantId}", ratesToAdd.Count, tenantId);
            }
            else
            {
                _logger.LogInformation("All VAT rates already exist for tenant {TenantId}", tenantId);
            }

            // Verify all expected rates are present
            var finalCount = await _dbContext.VatRates.CountAsync(v => v.TenantId == tenantId, cancellationToken);
            if (finalCount < 5)
            {
                _logger.LogWarning("Expected 5 VAT rates but found {Count} for tenant {TenantId}", finalCount, tenantId);
            }

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
    /// Ensures all required units exist, adding only missing ones.
    /// </summary>
    private async Task<bool> SeedUnitsOfMeasureAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding units of measure for tenant {TenantId}...", tenantId);

            // Get existing unit symbols for this tenant
            var existingSymbols = await _dbContext.UMs
                .Where(u => u.TenantId == tenantId)
                .Select(u => u.Symbol)
                .ToListAsync(cancellationToken);

            var existingSymbolsSet = existingSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingSymbolsSet.Count > 0)
            {
                _logger.LogInformation("Found {Count} existing units of measure for tenant {TenantId}", existingSymbolsSet.Count, tenantId);
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

            // Filter to only add missing units of measure
            var unitsToAdd = unitsOfMeasure.Where(um => !existingSymbolsSet.Contains(um.Symbol)).ToList();

            if (unitsToAdd.Any())
            {
                _dbContext.UMs.AddRange(unitsToAdd);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Added {Count} new units of measure for tenant {TenantId}", unitsToAdd.Count, tenantId);
            }
            else
            {
                _logger.LogInformation("All units of measure already exist for tenant {TenantId}", tenantId);
            }

            // Verify all expected units are present
            var finalCount = await _dbContext.UMs.CountAsync(u => u.TenantId == tenantId, cancellationToken);
            if (finalCount < 20)
            {
                _logger.LogWarning("Expected 20 units of measure but found {Count} for tenant {TenantId}", finalCount, tenantId);
            }

            // Ensure at least one default unit exists
            var hasDefault = await _dbContext.UMs.AnyAsync(u => u.TenantId == tenantId && u.IsDefault, cancellationToken);
            if (!hasDefault)
            {
                _logger.LogWarning("No default unit of measure found for tenant {TenantId}", tenantId);
            }

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
    /// Ensures default warehouse exists, creating it if missing.
    /// </summary>
    private async Task<bool> SeedDefaultWarehouseAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding default warehouse for tenant {TenantId}...", tenantId);

            // Check if a default warehouse already exists for this tenant
            var existingWarehouse = await _dbContext.StorageFacilities
                .Where(w => w.TenantId == tenantId && w.Code == "MAG-01")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingWarehouse != null)
            {
                _logger.LogInformation("Default warehouse '{WarehouseName}' already exists for tenant {TenantId}", existingWarehouse.Name, tenantId);

                // Check if default location exists for this warehouse
                var hasLocation = await _dbContext.StorageLocations
                    .AnyAsync(l => l.TenantId == tenantId && l.WarehouseId == existingWarehouse.Id, cancellationToken);

                if (!hasLocation)
                {
                    _logger.LogWarning("Default warehouse exists but has no storage locations. Creating default location...");
                    var newLocation = new StorageLocation
                    {
                        Code = "UB-DEF",
                        Description = "Ubicazione predefinita",
                        WarehouseId = existingWarehouse.Id,
                        IsActive = true,
                        TenantId = tenantId,
                        CreatedBy = "system",
                        CreatedAt = DateTime.UtcNow
                    };

                    _ = _dbContext.StorageLocations.Add(newLocation);
                    _ = await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Created default location '{LocationCode}' for warehouse {WarehouseId} in tenant {TenantId}",
                        newLocation.Code, existingWarehouse.Id, tenantId);
                }

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
    /// Ensures all required document types exist, adding only missing ones.
    /// </summary>
    private async Task<bool> SeedDocumentTypesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding document types for tenant {TenantId}...", tenantId);

            // Get existing document type codes for this tenant
            var existingCodes = await _dbContext.DocumentTypes
                .Where(dt => dt.TenantId == tenantId)
                .Select(dt => dt.Code)
                .ToListAsync(cancellationToken);

            var existingCodesSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingCodesSet.Count > 0)
            {
                _logger.LogInformation("Found {Count} existing document types for tenant {TenantId}", existingCodesSet.Count, tenantId);
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

            // Filter to only add missing document types
            var typesToAdd = documentTypes.Where(dt => !existingCodesSet.Contains(dt.Code)).ToList();

            if (typesToAdd.Any())
            {
                _dbContext.DocumentTypes.AddRange(typesToAdd);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Added {Count} new document types for tenant {TenantId}", typesToAdd.Count, tenantId);
            }
            else
            {
                _logger.LogInformation("All document types already exist for tenant {TenantId}", tenantId);
            }

            // Verify all expected document types are present
            var finalCount = await _dbContext.DocumentTypes.CountAsync(dt => dt.TenantId == tenantId, cancellationToken);
            if (finalCount < 12)
            {
                _logger.LogWarning("Expected 12 document types but found {Count} for tenant {TenantId}", finalCount, tenantId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding document types for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Validates that all required base entities exist for a tenant with correct TenantId assignments.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with details</returns>
    public async Task<(bool IsValid, List<string> Issues)> ValidateTenantBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        try
        {
            _logger.LogInformation("Validating base entities for tenant {TenantId}...", tenantId);

            // Validate VAT natures
            var vatNatureCount = await _dbContext.VatNatures
                .CountAsync(v => v.TenantId == tenantId, cancellationToken);
            if (vatNatureCount < 24)
            {
                issues.Add($"Expected 24 VAT natures but found {vatNatureCount}");
            }

            // Validate VAT rates
            var vatRateCount = await _dbContext.VatRates
                .CountAsync(v => v.TenantId == tenantId, cancellationToken);
            if (vatRateCount < 5)
            {
                issues.Add($"Expected 5 VAT rates but found {vatRateCount}");
            }

            // Validate units of measure
            var umCount = await _dbContext.UMs
                .CountAsync(u => u.TenantId == tenantId, cancellationToken);
            if (umCount < 20)
            {
                issues.Add($"Expected 20 units of measure but found {umCount}");
            }

            // Validate default unit exists
            var hasDefaultUM = await _dbContext.UMs
                .AnyAsync(u => u.TenantId == tenantId && u.IsDefault, cancellationToken);
            if (!hasDefaultUM)
            {
                issues.Add("No default unit of measure found");
            }

            // Validate warehouses
            var warehouseCount = await _dbContext.StorageFacilities
                .CountAsync(w => w.TenantId == tenantId, cancellationToken);
            if (warehouseCount == 0)
            {
                issues.Add("No warehouses found");
            }

            // Validate storage locations
            var locationCount = await _dbContext.StorageLocations
                .CountAsync(l => l.TenantId == tenantId, cancellationToken);
            if (locationCount == 0)
            {
                issues.Add("No storage locations found");
            }

            // Validate document types
            var docTypeCount = await _dbContext.DocumentTypes
                .CountAsync(dt => dt.TenantId == tenantId, cancellationToken);
            if (docTypeCount < 12)
            {
                issues.Add($"Expected 12 document types but found {docTypeCount}");
            }

            // Check for TenantId consistency - ensure no entities have wrong TenantId
            var wrongTenantIdVatNatures = await _dbContext.VatNatures
                .CountAsync(v => v.TenantId != tenantId && v.TenantId != Guid.Empty, cancellationToken);
            if (wrongTenantIdVatNatures > 0)
            {
                issues.Add($"Found {wrongTenantIdVatNatures} VAT natures with incorrect TenantId");
            }

            var isValid = issues.Count == 0;

            if (isValid)
            {
                _logger.LogInformation("Base entities validation passed for tenant {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Base entities validation found {IssueCount} issues for tenant {TenantId}: {Issues}",
                    issues.Count, tenantId, string.Join("; ", issues));
            }

            return (isValid, issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating base entities for tenant {TenantId}", tenantId);
            issues.Add($"Validation error: {ex.Message}");
            return (false, issues);
        }
    }
}
