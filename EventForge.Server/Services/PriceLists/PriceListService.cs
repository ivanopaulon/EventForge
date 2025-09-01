using EventForge.DTOs.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using ProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;

namespace EventForge.Server.Services.PriceLists;

public class PriceListService : IPriceListService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PriceListService> _logger;
    private readonly IUnitConversionService _unitConversionService;

    public PriceListService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<PriceListService> logger,
        IUnitConversionService unitConversionService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitConversionService = unitConversionService ?? throw new ArgumentNullException(nameof(unitConversionService));
    }

    public async Task<PagedResult<PriceListDto>> GetPriceListsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PriceLists
                .Where(pl => !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted));

            var totalCount = await query.CountAsync(cancellationToken);
            var priceLists = await query
                .OrderBy(pl => pl.Priority)
                .ThenBy(pl => pl.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var priceListDtos = priceLists.Select(MapToPriceListDto);

            return new PagedResult<PriceListDto>
            {
                Items = priceListDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price lists.");
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .OrderBy(pl => pl.Priority)
                .ThenBy(pl => pl.Name)
                .ToListAsync(cancellationToken);

            return priceLists.Select(MapToPriceListDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price lists for event {EventId}.", eventId);
            throw;
        }
    }

    public async Task<PriceListDto?> GetPriceListByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = await _context.PriceLists
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            return priceList != null ? MapToPriceListDto(priceList) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list {PriceListId}.", id);
            throw;
        }
    }

    public async Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = await _context.PriceLists
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            return priceList != null ? MapToPriceListDetailDto(priceList) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list detail {PriceListId}.", id);
            throw;
        }
    }

    public async Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto createPriceListDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPriceListDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            if (!await EventExistsAsync(createPriceListDto.EventId, cancellationToken))
            {
                throw new ArgumentException($"Event with ID {createPriceListDto.EventId} does not exist.");
            }

            var priceList = new Data.Entities.PriceList.PriceList
            {
                Name = createPriceListDto.Name,
                Description = createPriceListDto.Description,
                ValidFrom = createPriceListDto.ValidFrom,
                ValidTo = createPriceListDto.ValidTo,
                Notes = createPriceListDto.Notes,
                IsDefault = createPriceListDto.IsDefault,
                Priority = createPriceListDto.Priority,
                EventId = createPriceListDto.EventId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.PriceLists.Add(priceList);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(priceList, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Price list created with ID {PriceListId} by user {User}.", priceList.Id, currentUser);

            return MapToPriceListDto(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price list for user {User}.", currentUser);
            throw;
        }
    }

    public async Task<PriceListDto?> UpdatePriceListAsync(Guid id, UpdatePriceListDto updatePriceListDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePriceListDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPriceList = await _context.PriceLists
                .AsNoTracking()
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (originalPriceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var priceList = await _context.PriceLists
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (priceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            priceList.Name = updatePriceListDto.Name;
            priceList.Description = updatePriceListDto.Description;
            priceList.ValidFrom = updatePriceListDto.ValidFrom;
            priceList.ValidTo = updatePriceListDto.ValidTo;
            priceList.Notes = updatePriceListDto.Notes;
            priceList.IsDefault = updatePriceListDto.IsDefault;
            priceList.Priority = updatePriceListDto.Priority;
            priceList.ModifiedBy = currentUser;
            priceList.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(priceList, "Update", currentUser, originalPriceList, cancellationToken);

            _logger.LogInformation("Price list {PriceListId} updated by user {User}.", id, currentUser);

            return MapToPriceListDto(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price list {PriceListId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> DeletePriceListAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPriceList = await _context.PriceLists
                .AsNoTracking()
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (originalPriceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var priceList = await _context.PriceLists
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (priceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            priceList.IsDeleted = true;
            priceList.DeletedBy = currentUser;
            priceList.DeletedAt = DateTime.UtcNow;

            foreach (var entry in priceList.ProductPrices.Where(ple => !ple.IsDeleted))
            {
                entry.IsDeleted = true;
                entry.DeletedBy = currentUser;
                entry.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(priceList, "Delete", currentUser, originalPriceList, cancellationToken);

            _logger.LogInformation("Price list {PriceListId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting price list {PriceListId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<IEnumerable<PriceListEntryDto>> GetPriceListEntriesAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .OrderBy(ple => ple.ProductId)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToPriceListEntryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list entries for price list {PriceListId}.", priceListId);
            throw;
        }
    }

    public async Task<PriceListEntryDto?> GetPriceListEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _context.PriceListEntries
                .Where(ple => ple.Id == id && !ple.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return entry != null ? MapToPriceListEntryDto(entry) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list entry {EntryId}.", id);
            throw;
        }
    }

    public async Task<PriceListEntryDto> AddPriceListEntryAsync(CreatePriceListEntryDto createPriceListEntryDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPriceListEntryDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            if (!await PriceListExistsAsync(createPriceListEntryDto.PriceListId, cancellationToken))
            {
                throw new ArgumentException($"Price list with ID {createPriceListEntryDto.PriceListId} does not exist.");
            }

            if (!await ProductExistsAsync(createPriceListEntryDto.ProductId, cancellationToken))
            {
                throw new ArgumentException($"Product with ID {createPriceListEntryDto.ProductId} does not exist.");
            }

            var entry = new PriceListEntry
            {
                ProductId = createPriceListEntryDto.ProductId,
                PriceListId = createPriceListEntryDto.PriceListId,
                Price = createPriceListEntryDto.Price,
                Currency = createPriceListEntryDto.Currency,
                Score = createPriceListEntryDto.Score,
                IsEditableInFrontend = createPriceListEntryDto.IsEditableInFrontend,
                IsDiscountable = createPriceListEntryDto.IsDiscountable,
                MinQuantity = createPriceListEntryDto.MinQuantity,
                MaxQuantity = createPriceListEntryDto.MaxQuantity,
                Notes = createPriceListEntryDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entry, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Price list entry created with ID {EntryId} for price list {PriceListId} by user {User}.",
                entry.Id, createPriceListEntryDto.PriceListId, currentUser);

            return MapToPriceListEntryDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price list entry for price list {PriceListId} by user {User}.",
                createPriceListEntryDto.PriceListId, currentUser);
            throw;
        }
    }

    public async Task<PriceListEntryDto?> UpdatePriceListEntryAsync(Guid id, UpdatePriceListEntryDto updatePriceListEntryDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePriceListEntryDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntry = await _context.PriceListEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (originalEntry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entry = await _context.PriceListEntries
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (entry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            entry.Price = updatePriceListEntryDto.Price;
            entry.Currency = updatePriceListEntryDto.Currency;
            entry.Score = updatePriceListEntryDto.Score;
            entry.IsEditableInFrontend = updatePriceListEntryDto.IsEditableInFrontend;
            entry.IsDiscountable = updatePriceListEntryDto.IsDiscountable;
            entry.MinQuantity = updatePriceListEntryDto.MinQuantity;
            entry.MaxQuantity = updatePriceListEntryDto.MaxQuantity;
            entry.Notes = updatePriceListEntryDto.Notes;
            entry.ModifiedBy = currentUser;
            entry.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entry, "Update", currentUser, originalEntry, cancellationToken);

            _logger.LogInformation("Price list entry {EntryId} updated by user {User}.", id, currentUser);

            return MapToPriceListEntryDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price list entry {EntryId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> RemovePriceListEntryAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntry = await _context.PriceListEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (originalEntry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entry = await _context.PriceListEntries
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (entry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entry.IsDeleted = true;
            entry.DeletedBy = currentUser;
            entry.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entry, "Delete", currentUser, originalEntry, cancellationToken);

            _logger.LogInformation("Price list entry {EntryId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting price list entry {EntryId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> PriceListExistsAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PriceLists
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if price list {PriceListId} exists.", priceListId);
            throw;
        }
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if event {EventId} exists.", eventId);
            throw;
        }
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Products
                .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} exists.", productId);
            throw;
        }
    }

    // Private mapping methods

    private static PriceListDto MapToPriceListDto(Data.Entities.PriceList.PriceList priceList)
    {
        return new PriceListDto
        {
            Id = priceList.Id,
            Name = priceList.Name,
            Description = priceList.Description,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Notes = priceList.Notes,
            IsDefault = priceList.IsDefault,
            Priority = priceList.Priority,
            EventId = priceList.EventId,
            EntryCount = priceList.ProductPrices.Count(ple => !ple.IsDeleted),
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy,
            ModifiedAt = priceList.ModifiedAt,
            ModifiedBy = priceList.ModifiedBy
        };
    }

    private static PriceListDetailDto MapToPriceListDetailDto(Data.Entities.PriceList.PriceList priceList)
    {
        return new PriceListDetailDto
        {
            Id = priceList.Id,
            Name = priceList.Name,
            Description = priceList.Description,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Notes = priceList.Notes,
            IsDefault = priceList.IsDefault,
            Priority = priceList.Priority,
            EventId = priceList.EventId,
            Entries = priceList.ProductPrices.Where(ple => !ple.IsDeleted).Select(MapToPriceListEntryDto),
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy,
            ModifiedAt = priceList.ModifiedAt,
            ModifiedBy = priceList.ModifiedBy
        };
    }

    private static PriceListEntryDto MapToPriceListEntryDto(PriceListEntry entry)
    {
        return new PriceListEntryDto
        {
            Id = entry.Id,
            ProductId = entry.ProductId,
            PriceListId = entry.PriceListId,
            Price = entry.Price,
            Currency = entry.Currency,
            Score = entry.Score,
            IsEditableInFrontend = entry.IsEditableInFrontend,
            IsDiscountable = entry.IsDiscountable,
            MinQuantity = entry.MinQuantity,
            MaxQuantity = entry.MaxQuantity,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            CreatedBy = entry.CreatedBy,
            ModifiedAt = entry.ModifiedAt,
            ModifiedBy = entry.ModifiedBy
        };
    }

    // Enhanced price calculation methods (Issue #245)

    public async Task<AppliedPriceDto?> GetAppliedPriceAsync(Guid productId, Guid eventId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var evalDate = evaluationDate ?? DateTime.UtcNow;

            // Get all applicable price lists for the event with precedence ordering
            var applicablePriceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted && pl.Status == PriceListStatus.Active)
                .Where(pl => (pl.ValidFrom == null || pl.ValidFrom <= evalDate) &&
                           (pl.ValidTo == null || pl.ValidTo >= evalDate))
                .Include(pl => pl.ProductPrices.Where(ple => ple.ProductId == productId && !ple.IsDeleted &&
                                                            ple.Status == PriceListEntryStatus.Attivo &&
                                                            ple.MinQuantity <= quantity &&
                                                            (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity)))
                .OrderBy(pl => pl.Priority) // Higher priority (lower number) first
                .ThenBy(pl => pl.IsDefault ? 0 : 1) // Default price lists first within same priority
                .ThenByDescending(pl => pl.CreatedAt) // Newer price lists first as tiebreaker
                .ToListAsync(cancellationToken);

            // Find the first price list with a matching entry (precedence logic)
            var selectedEntry = applicablePriceLists
                .SelectMany(pl => pl.ProductPrices)
                .FirstOrDefault();

            if (selectedEntry == null)
            {
                _logger.LogWarning("No applicable price found for product {ProductId} in event {EventId} at date {EvaluationDate}",
                    productId, eventId, evalDate);
                return null;
            }

            var selectedPriceList = applicablePriceLists.First(pl => pl.ProductPrices.Any(ple => ple.Id == selectedEntry.Id));

            // Get unit information for the product's default unit
            var productWithUnit = await _context.Products
                .Include(p => p.Units.Where(pu => pu.UnitType == "Base" && !pu.IsDeleted))
                .ThenInclude(pu => pu.UnitOfMeasure)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);

            var defaultUnit = productWithUnit?.Units.FirstOrDefault();
            var unitOfMeasure = defaultUnit?.UnitOfMeasure;

            var result = new AppliedPriceDto
            {
                ProductId = productId,
                EventId = eventId,
                Price = selectedEntry.Price,
                Currency = selectedEntry.Currency,
                UnitOfMeasureId = unitOfMeasure?.Id ?? Guid.Empty,
                UnitOfMeasureName = unitOfMeasure?.Name ?? "Unknown",
                UnitSymbol = unitOfMeasure?.Symbol ?? "?",
                PriceListId = selectedPriceList.Id,
                PriceListName = selectedPriceList.Name,
                PriceListPriority = selectedPriceList.Priority,
                MinQuantity = selectedEntry.MinQuantity,
                MaxQuantity = selectedEntry.MaxQuantity,
                CalculatedAt = DateTime.UtcNow,
                IsEditableInFrontend = selectedEntry.IsEditableInFrontend,
                IsDiscountable = selectedEntry.IsDiscountable,
                Score = selectedEntry.Score,
                CalculationNotes = $"Price from '{selectedPriceList.Name}' (Priority: {selectedPriceList.Priority})"
            };

            _logger.LogInformation("Applied price {Price} {Currency} for product {ProductId} from price list '{PriceListName}' (Priority: {Priority})",
                result.Price, result.Currency, productId, selectedPriceList.Name, selectedPriceList.Priority);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating applied price for product {ProductId} in event {EventId}", productId, eventId);
            throw;
        }
    }

    public async Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            // First get the base applied price
            var basePrice = await GetAppliedPriceAsync(productId, eventId, evaluationDate, quantity, cancellationToken);
            if (basePrice == null)
            {
                return null;
            }

            // Get the target unit information
            var targetProductUnit = await _context.ProductUnits
                .Include(pu => pu.UnitOfMeasure)
                .FirstOrDefaultAsync(pu => pu.ProductId == productId &&
                                         pu.UnitOfMeasureId == targetUnitId &&
                                         !pu.IsDeleted &&
                                         pu.Status == ProductUnitStatus.Active,
                                         cancellationToken);

            if (targetProductUnit == null)
            {
                _logger.LogWarning("Target unit {TargetUnitId} not found for product {ProductId}", targetUnitId, productId);
                return basePrice; // Return base price if target unit not found
            }

            // Get the base unit information for conversion
            var baseProductUnit = await _context.ProductUnits
                .Include(pu => pu.UnitOfMeasure)
                .FirstOrDefaultAsync(pu => pu.ProductId == productId &&
                                         pu.UnitOfMeasureId == basePrice.UnitOfMeasureId &&
                                         !pu.IsDeleted &&
                                         pu.Status == ProductUnitStatus.Active,
                                         cancellationToken);

            if (baseProductUnit == null)
            {
                _logger.LogWarning("Base unit {BaseUnitId} not found for product {ProductId}", basePrice.UnitOfMeasureId, productId);
                return basePrice; // Return base price if conversion not possible
            }

            // Perform price conversion using the unit conversion service
            var convertedPrice = _unitConversionService.ConvertPrice(
                basePrice.Price,
                baseProductUnit.ConversionFactor,
                targetProductUnit.ConversionFactor,
                2); // 2 decimal places for currency

            // Create the result with conversion information
            var result = new AppliedPriceDto
            {
                ProductId = basePrice.ProductId,
                EventId = basePrice.EventId,
                Price = convertedPrice,
                Currency = basePrice.Currency,
                UnitOfMeasureId = targetUnitId,
                UnitOfMeasureName = targetProductUnit.UnitOfMeasure?.Name ?? "Unknown",
                UnitSymbol = targetProductUnit.UnitOfMeasure?.Symbol ?? "?",
                ConversionFactor = targetProductUnit.ConversionFactor,
                OriginalPrice = basePrice.Price,
                OriginalUnitOfMeasureId = basePrice.UnitOfMeasureId,
                PriceListId = basePrice.PriceListId,
                PriceListName = basePrice.PriceListName,
                PriceListPriority = basePrice.PriceListPriority,
                MinQuantity = basePrice.MinQuantity,
                MaxQuantity = basePrice.MaxQuantity,
                CalculatedAt = DateTime.UtcNow,
                IsEditableInFrontend = basePrice.IsEditableInFrontend,
                IsDiscountable = basePrice.IsDiscountable,
                Score = basePrice.Score,
                CalculationNotes = $"Price converted from {basePrice.UnitOfMeasureName} (factor: {baseProductUnit.ConversionFactor}) to {targetProductUnit.UnitOfMeasure?.Name} (factor: {targetProductUnit.ConversionFactor}). Original: {basePrice.Price:F2} {basePrice.Currency}"
            };

            _logger.LogInformation("Converted price from {OriginalPrice} {Currency}/{OriginalUnit} to {ConvertedPrice} {Currency}/{TargetUnit} for product {ProductId}",
                basePrice.Price, basePrice.Currency, basePrice.UnitOfMeasureName,
                convertedPrice, result.Currency, result.UnitOfMeasureName, productId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating applied price with unit conversion for product {ProductId} in event {EventId} to unit {TargetUnitId}",
                productId, eventId, targetUnitId);
            throw;
        }
    }

    // Placeholder implementations for remaining methods (to be implemented)
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement price history functionality
        await Task.CompletedTask;
        return new List<PriceHistoryDto>();
    }

    public async Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        // TODO: Implement bulk import functionality
        await Task.CompletedTask;
        return new BulkImportResultDto { PriceListId = priceListId };
    }

    public async Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
    {
        // TODO: Implement export functionality
        await Task.CompletedTask;
        return new List<ExportablePriceListEntryDto>();
    }

    public async Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement precedence validation
        await Task.CompletedTask;
        return new PrecedenceValidationResultDto { EventId = eventId };
    }
}