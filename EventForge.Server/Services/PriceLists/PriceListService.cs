using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using ProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;

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

            // Only validate EventId if it's provided
            if (createPriceListDto.EventId.HasValue && !await EventExistsAsync(createPriceListDto.EventId.Value, cancellationToken))
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

            _ = _context.PriceLists.Add(priceList);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(priceList, "Create", currentUser, null, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(priceList, "Update", currentUser, originalPriceList, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(priceList, "Delete", currentUser, originalPriceList, cancellationToken);

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

            _ = _context.PriceListEntries.Add(entry);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entry, "Create", currentUser, null, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entry, "Update", currentUser, originalEntry, cancellationToken);

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entry, "Delete", currentUser, originalEntry, cancellationToken);

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
            Code = priceList.Code,
            Description = priceList.Description,
            Type = priceList.Type,
            Direction = priceList.Direction,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Notes = priceList.Notes,
            Status = (EventForge.DTOs.Common.PriceListStatus)priceList.Status,
            IsDefault = priceList.IsDefault,
            Priority = priceList.Priority,
            EventId = priceList.EventId,
            EventName = priceList.Event?.Name,
            EntryCount = priceList.ProductPrices?.Count(ple => !ple.IsDeleted) ?? 0,
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy,
            ModifiedAt = priceList.ModifiedAt,
            ModifiedBy = priceList.ModifiedBy,
            AssignedBusinessParties = priceList.BusinessParties
                .Where(bp => !bp.IsDeleted && bp.Status == PriceListBusinessPartyStatus.Active)
                .Select(bp => new PriceListBusinessPartyDto
                {
                    BusinessPartyId = bp.BusinessPartyId,
                    BusinessPartyName = bp.BusinessParty?.Name ?? "Unknown",
                    BusinessPartyType = bp.BusinessParty?.PartyType.ToString() ?? "Unknown",
                    IsPrimary = bp.IsPrimary,
                    OverridePriority = bp.OverridePriority,
                    SpecificValidFrom = bp.SpecificValidFrom,
                    SpecificValidTo = bp.SpecificValidTo,
                    GlobalDiscountPercentage = bp.GlobalDiscountPercentage,
                    Notes = bp.Notes,
                    Status = bp.Status.ToString()
                })
                .ToList()
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

    public async Task<AppliedPriceDto?> GetAppliedPriceAsync(
        Guid productId,
        Guid eventId,
        Guid? businessPartyId = null,
        DateTime? evaluationDate = null,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evalDate = evaluationDate ?? DateTime.UtcNow;

            // Step 1: Trova listini vendita applicabili
            var query = _context.PriceLists
                .Where(pl => pl.Type == PriceListType.Sales &&
                             pl.Direction == PriceListDirection.Output &&
                             !pl.IsDeleted &&
                             pl.Status == PriceListStatus.Active)
                .Where(pl => pl.EventId == eventId)
                .Where(pl => (pl.ValidFrom == null || pl.ValidFrom <= evalDate) &&
                             (pl.ValidTo == null || pl.ValidTo >= evalDate))
                .Include(pl => pl.BusinessParties.Where(bp =>
                    !bp.IsDeleted &&
                    bp.Status == PriceListBusinessPartyStatus.Active))
                .ThenInclude(bp => bp.BusinessParty)
                .Include(pl => pl.ProductPrices.Where(ple =>
                    ple.ProductId == productId &&
                    !ple.IsDeleted &&
                    ple.Status == PriceListEntryStatus.Active &&
                    ple.MinQuantity <= quantity &&
                    (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity)));

            var applicablePriceLists = await query.ToListAsync(cancellationToken);

            // Step 2: Filtra per BusinessParty se specificato
            if (businessPartyId.HasValue)
            {
                applicablePriceLists = applicablePriceLists
                    .Where(pl =>
                        // Listino generico (senza BusinessParty assegnati)
                        !pl.BusinessParties.Any() ||
                        // Oppure listino assegnato a questo BusinessParty
                        pl.BusinessParties.Any(bp =>
                            bp.BusinessPartyId == businessPartyId.Value &&
                            (!bp.SpecificValidFrom.HasValue || bp.SpecificValidFrom.Value <= evalDate) &&
                            (!bp.SpecificValidTo.HasValue || bp.SpecificValidTo.Value >= evalDate)))
                    .ToList();
            }
            else
            {
                // Solo listini generici (senza BusinessParty)
                applicablePriceLists = applicablePriceLists
                    .Where(pl => !pl.BusinessParties.Any())
                    .ToList();
            }

            // Step 3: Ordina per precedenza
            var orderedPriceList = applicablePriceLists
                .SelectMany(pl => pl.ProductPrices.Select(ple => new
                {
                    PriceList = pl,
                    Entry = ple,
                    BusinessPartyRel = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                        : null,
                    EffectivePriority = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                            ?.OverridePriority ?? pl.Priority
                        : pl.Priority,
                    GlobalDiscount = businessPartyId.HasValue
                        ? pl.BusinessParties.FirstOrDefault(bp => bp.BusinessPartyId == businessPartyId.Value)
                            ?.GlobalDiscountPercentage
                        : null
                }))
                .OrderBy(x => x.EffectivePriority)  // Priorità più bassa = più importante
                .ThenBy(x => x.PriceList.IsDefault ? 0 : 1)
                .ThenByDescending(x => x.PriceList.CreatedAt)
                .FirstOrDefault();

            if (orderedPriceList == null)
                return null;

            // Step 4: Calcola prezzo finale con sconto globale
            var finalPrice = orderedPriceList.Entry.Price;
            if (orderedPriceList.GlobalDiscount.HasValue)
            {
                finalPrice *= (1 - orderedPriceList.GlobalDiscount.Value / 100m);
            }

            return new AppliedPriceDto
            {
                ProductId = productId,
                EventId = eventId,
                Price = finalPrice,
                OriginalPrice = orderedPriceList.Entry.Price,
                Currency = orderedPriceList.Entry.Currency,
                PriceListId = orderedPriceList.PriceList.Id,
                PriceListName = orderedPriceList.PriceList.Name,
                PriceListPriority = orderedPriceList.EffectivePriority,
                MinQuantity = orderedPriceList.Entry.MinQuantity,
                MaxQuantity = orderedPriceList.Entry.MaxQuantity,
                IsEditableInFrontend = orderedPriceList.Entry.IsEditableInFrontend,
                IsDiscountable = orderedPriceList.Entry.IsDiscountable,
                Score = orderedPriceList.Entry.Score,
                CalculatedAt = DateTime.UtcNow,
                BusinessPartyId = orderedPriceList.BusinessPartyRel?.BusinessPartyId,
                BusinessPartyName = orderedPriceList.BusinessPartyRel?.BusinessParty?.Name,
                AppliedDiscountPercentage = orderedPriceList.GlobalDiscount,
                UnitOfMeasureId = Guid.Empty, // TODO: Gestire UnitOfMeasure se necessario
                UnitOfMeasureName = string.Empty,
                UnitSymbol = string.Empty,
                CalculationNotes = orderedPriceList.GlobalDiscount.HasValue
                    ? $"Applied {orderedPriceList.GlobalDiscount.Value:F2}% global discount for {orderedPriceList.BusinessPartyRel?.BusinessParty?.Name}"
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating applied price for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            // First get the base applied price
            var basePrice = await GetAppliedPriceAsync(productId, eventId, null, evaluationDate, quantity, cancellationToken);
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

    // Enhanced methods implementation (Issue #245)
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddYears(-1); // Default to last year
            var to = toDate ?? DateTime.UtcNow;

            // Get all price lists for the event
            var priceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => ple.ProductId == productId && !ple.IsDeleted))
                .ToListAsync(cancellationToken);

            var historyEntries = new List<PriceHistoryDto>();

            foreach (var priceList in priceLists)
            {
                foreach (var entry in priceList.ProductPrices)
                {
                    // Determine effective date range
                    var effectiveFrom = priceList.ValidFrom ?? entry.CreatedAt;
                    var effectiveTo = priceList.ValidTo;

                    // Skip entries outside the requested date range
                    if (effectiveTo.HasValue && effectiveTo.Value < from)
                        continue;
                    if (effectiveFrom > to)
                        continue;

                    var wasActive = priceList.Status == PriceListStatus.Active &&
                                   entry.Status == PriceListEntryStatus.Active &&
                                   (!priceList.ValidFrom.HasValue || priceList.ValidFrom.Value <= to) &&
                                   (!priceList.ValidTo.HasValue || priceList.ValidTo.Value >= from);

                    historyEntries.Add(new PriceHistoryDto
                    {
                        ProductId = productId,
                        EventId = eventId,
                        PriceListId = priceList.Id,
                        PriceListName = priceList.Name,
                        Price = entry.Price,
                        Currency = entry.Currency,
                        EffectiveFrom = effectiveFrom,
                        EffectiveTo = effectiveTo,
                        Priority = priceList.Priority,
                        IsDefault = priceList.IsDefault,
                        CreatedAt = entry.CreatedAt,
                        CreatedBy = entry.CreatedBy,
                        ModifiedAt = entry.ModifiedAt,
                        ModifiedBy = entry.ModifiedBy,
                        MinQuantity = entry.MinQuantity,
                        MaxQuantity = entry.MaxQuantity,
                        WasActive = wasActive,
                        Notes = entry.Notes
                    });
                }
            }

            // Order by effective date descending, then by priority
            var result = historyEntries
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenBy(h => h.Priority)
                .ToList();

            _logger.LogInformation("Retrieved {Count} price history entries for product {ProductId} in event {EventId}",
                result.Count, productId, eventId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for product {ProductId} in event {EventId}",
                productId, eventId);
            throw;
        }
    }

    public async Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new BulkImportResultDto
        {
            PriceListId = priceListId,
            ImportedBy = currentUser,
            ReplacedExisting = replaceExisting
        };

        try
        {
            // Verify price list exists
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (priceList == null)
            {
                result.Errors.Add(new BulkImportErrorDto
                {
                    RowIndex = 0,
                    ErrorCode = "PRICELIST_NOT_FOUND",
                    ErrorMessage = $"Price list {priceListId} not found"
                });
                result.FailureCount = 1;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var entriesList = entries.ToList();
            result.TotalProcessed = entriesList.Count;

            var rowIndex = 0;
            foreach (var entryDto in entriesList)
            {
                rowIndex++;

                try
                {
                    // Validate product exists
                    var productExists = await _context.Products
                        .AnyAsync(p => p.Id == entryDto.ProductId && !p.IsDeleted, cancellationToken);

                    if (!productExists)
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowIndex = rowIndex,
                            ProductId = entryDto.ProductId,
                            ErrorCode = "PRODUCT_NOT_FOUND",
                            ErrorMessage = $"Product {entryDto.ProductId} not found",
                            FieldName = nameof(entryDto.ProductId)
                        });
                        result.FailureCount++;
                        result.SkippedCount++;
                        continue;
                    }

                    // Check if entry already exists
                    var existingEntry = await _context.PriceListEntries
                        .FirstOrDefaultAsync(ple => ple.PriceListId == priceListId &&
                                                   ple.ProductId == entryDto.ProductId &&
                                                   !ple.IsDeleted,
                                                   cancellationToken);

                    if (existingEntry != null)
                    {
                        if (replaceExisting)
                        {
                            // Update existing entry
                            existingEntry.Price = entryDto.Price;
                            existingEntry.Currency = entryDto.Currency;
                            existingEntry.Score = entryDto.Score;
                            existingEntry.IsEditableInFrontend = entryDto.IsEditableInFrontend;
                            existingEntry.IsDiscountable = entryDto.IsDiscountable;
                            existingEntry.MinQuantity = entryDto.MinQuantity;
                            existingEntry.MaxQuantity = entryDto.MaxQuantity;
                            existingEntry.Notes = entryDto.Notes;
                            existingEntry.ModifiedAt = DateTime.UtcNow;
                            existingEntry.ModifiedBy = currentUser;

                            result.UpdatedCount++;
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.Warnings.Add(new BulkImportWarningDto
                            {
                                RowIndex = rowIndex,
                                ProductId = entryDto.ProductId,
                                WarningCode = "DUPLICATE_ENTRY",
                                WarningMessage = $"Entry for product {entryDto.ProductId} already exists",
                                ActionTaken = "Skipped"
                            });
                            result.SkippedCount++;
                            continue;
                        }
                    }
                    else
                    {
                        // Create new entry
                        var newEntry = new PriceListEntry
                        {
                            Id = Guid.NewGuid(),
                            PriceListId = priceListId,
                            ProductId = entryDto.ProductId,
                            Price = entryDto.Price,
                            Currency = entryDto.Currency,
                            Score = entryDto.Score,
                            IsEditableInFrontend = entryDto.IsEditableInFrontend,
                            IsDiscountable = entryDto.IsDiscountable,
                            Status = PriceListEntryStatus.Active,
                            MinQuantity = entryDto.MinQuantity,
                            MaxQuantity = entryDto.MaxQuantity,
                            Notes = entryDto.Notes,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUser
                        };

                        _ = _context.PriceListEntries.Add(newEntry);
                        result.CreatedCount++;
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing price list entry at row {RowIndex}", rowIndex);
                    result.Errors.Add(new BulkImportErrorDto
                    {
                        RowIndex = rowIndex,
                        ProductId = entryDto.ProductId,
                        ErrorCode = "IMPORT_ERROR",
                        ErrorMessage = ex.Message
                    });
                    result.FailureCount++;
                }
            }

            // Save all changes
            if (result.SuccessCount > 0)
            {
                _ = await _context.SaveChangesAsync(cancellationToken);

                _ = await _auditLogService.LogEntityChangeAsync(
                    "PriceList",
                    priceListId,
                    "BulkImport",
                    replaceExisting ? "BulkUpdate" : "BulkImport",
                    null,
                    $"Bulk import: {result.SuccessCount} entries imported/updated, {result.FailureCount} failed",
                    currentUser,
                    priceList.Name,
                    cancellationToken);
            }

            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Bulk import completed for price list {PriceListId}: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                priceListId, result.SuccessCount, result.FailureCount, result.SkippedCount, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk import for price list {PriceListId}", priceListId);
            result.Duration = DateTime.UtcNow - startTime;
            throw;
        }
    }

    public async Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get price list entries with related product information
            var query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product!)
                .ThenInclude(p => p.CategoryNode)
                .Include(ple => ple.Product!)
                .ThenInclude(p => p.Units.Where(pu => pu.UnitType == "Base" && !pu.IsDeleted))
                .ThenInclude(pu => pu.UnitOfMeasure)
                .AsQueryable();

            if (!includeInactiveEntries)
            {
                query = query.Where(ple => ple.Status == PriceListEntryStatus.Active);
            }

            var entries = await query.ToListAsync(cancellationToken);

            var exportableEntries = entries.Select(entry =>
            {
                var product = entry.Product;
                var baseUnit = product?.Units.FirstOrDefault();
                var unitOfMeasure = baseUnit?.UnitOfMeasure;

                return new ExportablePriceListEntryDto
                {
                    Id = entry.Id,
                    ProductId = entry.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    ProductCode = product?.Code,
                    ProductSku = product?.Code, // Product uses Code property
                    PriceListId = entry.PriceListId,
                    Price = entry.Price,
                    Currency = entry.Currency,
                    Score = entry.Score,
                    IsEditableInFrontend = entry.IsEditableInFrontend,
                    IsDiscountable = entry.IsDiscountable,
                    Status = entry.Status.ToString(),
                    MinQuantity = entry.MinQuantity,
                    MaxQuantity = entry.MaxQuantity,
                    Notes = entry.Notes,
                    CreatedAt = entry.CreatedAt,
                    CreatedBy = entry.CreatedBy,
                    ModifiedAt = entry.ModifiedAt,
                    ModifiedBy = entry.ModifiedBy,
                    IsActive = entry.Status == PriceListEntryStatus.Active,
                    ProductCategory = product?.CategoryNode?.Name,
                    UnitOfMeasure = unitOfMeasure?.Symbol,
                    ProductDefaultPrice = product?.DefaultPrice
                };
            }).ToList();

            _logger.LogInformation("Exported {Count} price list entries from price list {PriceListId}",
                exportableEntries.Count, priceListId);

            return exportableEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting price list entries for price list {PriceListId}", priceListId);
            throw;
        }
    }

    public async Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new PrecedenceValidationResultDto
        {
            EventId = eventId
        };

        try
        {
            // Get all price lists for the event
            var priceLists = await _context.PriceLists
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .ToListAsync(cancellationToken);

            result.TotalPriceListsValidated = priceLists.Count;

            if (priceLists.Count == 0)
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.NoPriceListsFound,
                    Severity = ValidationSeverity.Critical,
                    Description = "No price lists found for this event",
                    SuggestedResolution = "Create at least one price list for the event",
                    Impact = "Products cannot be priced for this event"
                });
                result.ValidationDuration = DateTime.UtcNow - startTime;
                return result;
            }

            var activePriceLists = priceLists.Where(pl => pl.Status == PriceListStatus.Active).ToList();
            var defaultPriceLists = priceLists.Where(pl => pl.IsDefault).ToList();
            var now = DateTime.UtcNow;
            var expiredPriceLists = priceLists.Where(pl => pl.ValidTo.HasValue && pl.ValidTo.Value < now).ToList();

            result.ActivePriceListsCount = activePriceLists.Count;
            result.DefaultPriceListsCount = defaultPriceLists.Count;
            result.ExpiredPriceListsCount = expiredPriceLists.Count;

            // Validation 1: Check for multiple default price lists
            if (defaultPriceLists.Count > 1)
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.MultipleDefaultPriceLists,
                    Severity = ValidationSeverity.High,
                    Description = $"Multiple default price lists found ({defaultPriceLists.Count})",
                    AffectedPriceListIds = defaultPriceLists.Select(pl => pl.Id).ToList(),
                    AffectedPriceListNames = defaultPriceLists.Select(pl => pl.Name).ToList(),
                    SuggestedResolution = "Set only one price list as default",
                    Impact = "Ambiguous default price selection may cause inconsistent pricing"
                });
            }

            // Validation 2: Check for no default price list (warning)
            if (defaultPriceLists.Count == 0 && activePriceLists.Count > 0)
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.UnusualPriorityRange,
                    Description = "No default price list found",
                    AffectedPriceListIds = activePriceLists.Select(pl => pl.Id).ToList(),
                    Recommendation = "Consider setting one price list as default for fallback pricing"
                });
            }

            // Validation 3: Check for conflicting priorities (same priority values)
            var priorityGroups = activePriceLists.GroupBy(pl => pl.Priority).Where(g => g.Count() > 1).ToList();
            if (priorityGroups.Any())
            {
                foreach (var group in priorityGroups)
                {
                    result.Warnings.Add(new PrecedenceValidationWarningDto
                    {
                        WarningType = PrecedenceWarningType.DuplicatePriorities,
                        Description = $"Multiple price lists have priority {group.Key}",
                        AffectedPriceListIds = group.Select(pl => pl.Id).ToList(),
                        Recommendation = "Consider assigning unique priority values for clearer precedence"
                    });
                }
            }

            // Validation 4: Check if only expired price lists exist
            if (activePriceLists.Count > 0 && activePriceLists.All(pl => pl.ValidTo.HasValue && pl.ValidTo.Value < now))
            {
                result.IsValid = false;
                result.Issues.Add(new PrecedenceValidationIssueDto
                {
                    IssueType = PrecedenceIssueType.ExpiredPriceListsOnly,
                    Severity = ValidationSeverity.Critical,
                    Description = "All active price lists have expired",
                    AffectedPriceListIds = activePriceLists.Select(pl => pl.Id).ToList(),
                    AffectedPriceListNames = activePriceLists.Select(pl => pl.Name).ToList(),
                    SuggestedResolution = "Extend validity dates or create new price lists",
                    Impact = "No valid prices available for current date"
                });
            }

            // Validation 5: Check for price lists expiring soon (within 7 days)
            var soonToExpire = activePriceLists
                .Where(pl => pl.ValidTo.HasValue &&
                           pl.ValidTo.Value >= now &&
                           pl.ValidTo.Value <= now.AddDays(7))
                .ToList();

            if (soonToExpire.Any())
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.SoonToExpire,
                    Description = $"{soonToExpire.Count} price list(s) expiring within 7 days",
                    AffectedPriceListIds = soonToExpire.Select(pl => pl.Id).ToList(),
                    Recommendation = "Review and extend validity dates or prepare replacement price lists"
                });
            }

            // Validation 6: Check for too many active price lists (warning only)
            if (activePriceLists.Count > 10)
            {
                result.Warnings.Add(new PrecedenceValidationWarningDto
                {
                    WarningType = PrecedenceWarningType.ManyActivePriceLists,
                    Description = $"Large number of active price lists ({activePriceLists.Count})",
                    Recommendation = "Consider consolidating or archiving unused price lists for better performance"
                });
            }

            // Validation 7: Check for overlapping validity periods with same priority
            foreach (var group in priorityGroups)
            {
                var sortedByDate = group.OrderBy(pl => pl.ValidFrom ?? DateTime.MinValue).ToList();
                for (int i = 0; i < sortedByDate.Count - 1; i++)
                {
                    var current = sortedByDate[i];
                    var next = sortedByDate[i + 1];

                    var currentEnd = current.ValidTo ?? DateTime.MaxValue;
                    var nextStart = next.ValidFrom ?? DateTime.MinValue;

                    if (currentEnd >= nextStart)
                    {
                        result.Issues.Add(new PrecedenceValidationIssueDto
                        {
                            IssueType = PrecedenceIssueType.OverlappingValidityPeriods,
                            Severity = ValidationSeverity.Medium,
                            Description = $"Price lists '{current.Name}' and '{next.Name}' have overlapping validity periods with same priority",
                            AffectedPriceListIds = new List<Guid> { current.Id, next.Id },
                            AffectedPriceListNames = new List<string> { current.Name, next.Name },
                            SuggestedResolution = "Adjust validity dates or priorities to avoid ambiguity",
                            Impact = "Ambiguous price selection during overlap period"
                        });
                        result.IsValid = false;
                    }
                }
            }

            // Set recommended default price list
            if (defaultPriceLists.Count == 1)
            {
                result.RecommendedDefaultPriceListId = defaultPriceLists[0].Id;
                result.RecommendedDefaultPriceListName = defaultPriceLists[0].Name;
            }
            else if (activePriceLists.Any())
            {
                // Recommend the highest priority (lowest number) active price list
                var recommended = activePriceLists.OrderBy(pl => pl.Priority).First();
                result.RecommendedDefaultPriceListId = recommended.Id;
                result.RecommendedDefaultPriceListName = recommended.Name;
            }

            result.ValidationDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Precedence validation completed for event {EventId}: {IsValid}, {IssueCount} issues, {WarningCount} warnings in {Duration}ms",
                eventId, result.IsValid, result.Issues.Count, result.Warnings.Count, result.ValidationDuration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price list precedence for event {EventId}", eventId);
            result.ValidationDuration = DateTime.UtcNow - startTime;
            throw;
        }
    }

    #region Purchase Price Comparison

    public async Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(
        Guid productId,
        int quantity = 1,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evalDate = evaluationDate ?? DateTime.UtcNow;

            // Trova tutti i listini acquisto applicabili
            var purchasePriceLists = await _context.PriceLists
                .Where(pl => pl.Type == PriceListType.Purchase &&
                             pl.Direction == PriceListDirection.Input &&
                             !pl.IsDeleted &&
                             pl.Status == PriceListStatus.Active)
                .Where(pl => (pl.ValidFrom == null || pl.ValidFrom <= evalDate) &&
                             (pl.ValidTo == null || pl.ValidTo >= evalDate))
                .Include(pl => pl.BusinessParties.Where(bp =>
                    !bp.IsDeleted &&
                    bp.Status == PriceListBusinessPartyStatus.Active &&
                    (bp.SpecificValidFrom == null || bp.SpecificValidFrom <= evalDate) &&
                    (bp.SpecificValidTo == null || bp.SpecificValidTo >= evalDate)))
                .ThenInclude(bp => bp.BusinessParty)
                .Include(pl => pl.ProductPrices.Where(ple =>
                    ple.ProductId == productId &&
                    !ple.IsDeleted &&
                    ple.Status == PriceListEntryStatus.Active &&
                    ple.MinQuantity <= quantity &&
                    (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity)))
                .ToListAsync(cancellationToken);

            var comparisons = new List<PurchasePriceComparisonDto>();

            foreach (var priceList in purchasePriceLists)
            {
                var entry = priceList.ProductPrices.FirstOrDefault();
                if (entry == null) continue;

                // Se non ci sono BusinessParty assegnati, salta questo listino
                if (!priceList.BusinessParties.Any()) continue;

                foreach (var businessPartyRel in priceList.BusinessParties)
                {
                    var effectivePrice = entry.Price;
                    decimal? discountPercentage = null;

                    // Applica sconto globale se presente
                    if (businessPartyRel.GlobalDiscountPercentage.HasValue)
                    {
                        discountPercentage = businessPartyRel.GlobalDiscountPercentage.Value;
                        effectivePrice *= (1 - discountPercentage.Value / 100m);
                    }

                    comparisons.Add(new PurchasePriceComparisonDto
                    {
                        ProductId = productId,
                        SupplierId = businessPartyRel.BusinessPartyId,
                        SupplierName = businessPartyRel.BusinessParty?.Name ?? "Unknown",
                        PriceListId = priceList.Id,
                        PriceListName = priceList.Name,
                        Price = effectivePrice,
                        OriginalPrice = entry.Price,
                        Currency = entry.Currency,
                        LeadTimeDays = entry.LeadTimeDays,
                        MinimumOrderQuantity = entry.MinimumOrderQuantity,
                        QuantityIncrement = entry.QuantityIncrement,
                        SupplierProductCode = entry.SupplierProductCode,
                        IsPrimarySupplier = businessPartyRel.IsPrimary,
                        Priority = businessPartyRel.OverridePriority ?? priceList.Priority,
                        AppliedDiscountPercentage = discountPercentage
                    });
                }
            }

            // Ordina per prezzo (migliore prima)
            return comparisons.OrderBy(c => c.Price).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing purchase prices for product {ProductId}", productId);
            throw;
        }
    }

    #endregion

    #region Phase 2A/2B - BusinessParty Assignment Methods (Stub implementations)

    public async Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        // Stub implementation - to be completed in Phase 2A/2B PR
        throw new NotImplementedException("AssignBusinessPartyAsync will be implemented in Phase 2A/2B");
    }

    public async Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default)
    {
        // Stub implementation - to be completed in Phase 2A/2B PR
        throw new NotImplementedException("RemoveBusinessPartyAsync will be implemented in Phase 2A/2B");
    }

    public async Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        // Stub implementation - to be completed in Phase 2A/2B PR
        throw new NotImplementedException("GetBusinessPartiesForPriceListAsync will be implemented in Phase 2A/2B");
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByTypeAsync(PriceListType type, CancellationToken cancellationToken = default)
    {
        // Stub implementation - to be completed in Phase 2A/2B PR
        throw new NotImplementedException("GetPriceListsByTypeAsync will be implemented in Phase 2A/2B");
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default)
    {
        // Stub implementation - to be completed in Phase 2A/2B PR
        throw new NotImplementedException("GetPriceListsByBusinessPartyAsync will be implemented in Phase 2A/2B");
    }

    #endregion

    #region Phase 2C - Product Price Calculation with Application Modes

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// </summary>
    public async Task<ProductPriceResultDto> GetProductPriceAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Recupera il prodotto
            var product = await _context.Products
                .Include(p => p.Codes)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID {request.ProductId} not found");
            }

            // 2. Determina la modalità di applicazione
            var mode = await DeterminePriceApplicationModeAsync(request, cancellationToken);

            // 3. Applica la strategia corretta
            return mode switch
            {
                PriceApplicationMode.Manual => await ApplyManualPriceAsync(request, product, cancellationToken),
                PriceApplicationMode.ForcedPriceList => await ApplyForcedPriceListAsync(request, product, cancellationToken),
                PriceApplicationMode.HybridForcedWithOverrides => await ApplyHybridPriceAsync(request, product, cancellationToken),
                PriceApplicationMode.Automatic => await ApplyAutomaticPriceAsync(request, product, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown price application mode: {mode}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating product price for ProductId {ProductId}", request.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Determina quale modalità usare per il calcolo del prezzo.
    /// </summary>
    private async Task<PriceApplicationMode> DeterminePriceApplicationModeAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken)
    {
        // 1. Override esplicito nella richiesta → priorità massima
        if (request.PriceApplicationMode.HasValue)
        {
            _logger.LogDebug("Using explicit price application mode from request: {Mode}", request.PriceApplicationMode.Value);
            return request.PriceApplicationMode.Value;
        }

        // 2. Nessun BusinessParty → Automatic
        if (!request.BusinessPartyId.HasValue)
        {
            _logger.LogDebug("No BusinessParty specified, using Automatic mode");
            return PriceApplicationMode.Automatic;
        }

        // 3. Configurazione del BusinessParty
        var businessParty = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == request.BusinessPartyId.Value && !bp.IsDeleted, cancellationToken);

        if (businessParty == null)
        {
            _logger.LogWarning("BusinessParty {BusinessPartyId} not found, using Automatic mode", request.BusinessPartyId.Value);
            return PriceApplicationMode.Automatic;
        }

        _logger.LogDebug("Using BusinessParty default price application mode: {Mode}", businessParty.DefaultPriceApplicationMode);
        return businessParty.DefaultPriceApplicationMode;
    }

    /// <summary>
    /// Gestisce modalità Manual: usa il prezzo manuale fornito.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyManualPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        if (!request.ManualPrice.HasValue || request.ManualPrice.Value <= 0)
        {
            throw new InvalidOperationException("ManualPrice is required and must be greater than 0 when using Manual mode");
        }

        _logger.LogInformation("Applying manual price {Price} for product {ProductId}", request.ManualPrice.Value, product.Id);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = request.ManualPrice.Value,
            Currency = "EUR",
            AppliedMode = PriceApplicationMode.Manual,
            IsManual = true,
            IsPriceListForced = false,
            SearchPath = new List<string> { "Manual price specified in request" }
        };
    }

    /// <summary>
    /// Gestisce modalità ForcedPriceList: cerca il prezzo nel listino forzato.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyForcedPriceListAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        // Determina quale listino forzare (da request o da BusinessParty)
        Guid? forcedPriceListId = request.ForcedPriceListId;

        if (!forcedPriceListId.HasValue && request.BusinessPartyId.HasValue)
        {
            var businessParty = await _context.BusinessParties
                .FirstOrDefaultAsync(bp => bp.Id == request.BusinessPartyId.Value && !bp.IsDeleted, cancellationToken);

            forcedPriceListId = businessParty?.ForcedPriceListId;
        }

        if (!forcedPriceListId.HasValue)
        {
            throw new InvalidOperationException("ForcedPriceListId is required when using ForcedPriceList mode");
        }

        var evaluationDate = request.ReferenceDate ?? DateTime.UtcNow;

        // Cerca il prezzo nel listino forzato
        var priceEntry = await _context.PriceListEntries
            .Include(e => e.PriceList)
            .Where(e => e.PriceListId == forcedPriceListId.Value
                     && e.ProductId == product.Id
                     && !e.IsDeleted
                     && e.PriceList!.Status == PriceListStatus.Active
                     && (!e.PriceList.ValidFrom.HasValue || e.PriceList.ValidFrom <= evaluationDate)
                     && (!e.PriceList.ValidTo.HasValue || e.PriceList.ValidTo >= evaluationDate)
                     && (e.MinQuantity == 0 || e.MinQuantity <= request.Quantity)
                     && (e.MaxQuantity == 0 || e.MaxQuantity >= request.Quantity))
            .FirstOrDefaultAsync(cancellationToken);

        if (priceEntry == null)
        {
            throw new InvalidOperationException($"Product {product.Id} not found in forced price list {forcedPriceListId.Value}");
        }

        var basePrice = priceEntry.Price;
        var finalPrice = basePrice;
        decimal? discountPercentage = null;

        // Applica eventuale sconto BusinessParty
        if (request.BusinessPartyId.HasValue)
        {
            var businessPartyRelation = await _context.PriceListBusinessParties
                .FirstOrDefaultAsync(plbp => plbp.PriceListId == forcedPriceListId.Value
                                          && plbp.BusinessPartyId == request.BusinessPartyId.Value
                                          && !plbp.IsDeleted
                                          && plbp.Status == PriceListBusinessPartyStatus.Active,
                                          cancellationToken);

            if (businessPartyRelation?.GlobalDiscountPercentage.HasValue == true)
            {
                discountPercentage = businessPartyRelation.GlobalDiscountPercentage.Value;
                finalPrice = basePrice * (1 - discountPercentage.Value / 100m);
            }
        }

        _logger.LogInformation("Applied forced price list {PriceListId} for product {ProductId}: {Price}", 
            forcedPriceListId.Value, product.Id, finalPrice);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = finalPrice,
            Currency = priceEntry.Currency,
            AppliedMode = PriceApplicationMode.ForcedPriceList,
            AppliedPriceListId = priceEntry.PriceListId,
            AppliedPriceListName = priceEntry.PriceList?.Name,
            BasePriceFromPriceList = basePrice,
            AppliedDiscountPercentage = discountPercentage,
            PriceAfterDiscount = discountPercentage.HasValue ? finalPrice : null,
            IsManual = false,
            IsPriceListForced = true,
            SearchPath = new List<string> { $"Forced price list: {priceEntry.PriceList?.Name}" }
        };
    }

    /// <summary>
    /// Gestisce modalità Automatic: cerca il listino migliore secondo precedenza.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyAutomaticPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        var evaluationDate = request.ReferenceDate ?? DateTime.UtcNow;
        var searchPath = new List<string>();

        // Recupera tutti i listini applicabili (attivi, validi per data)
        var applicablePriceEntries = await _context.PriceListEntries
            .Include(e => e.PriceList)
                .ThenInclude(pl => pl!.BusinessParties.Where(bp => !bp.IsDeleted))
            .Where(e => e.ProductId == product.Id
                     && !e.IsDeleted
                     && e.PriceList!.Status == PriceListStatus.Active
                     && (!e.PriceList.ValidFrom.HasValue || e.PriceList.ValidFrom <= evaluationDate)
                     && (!e.PriceList.ValidTo.HasValue || e.PriceList.ValidTo >= evaluationDate)
                     && (e.MinQuantity == 0 || e.MinQuantity <= request.Quantity)
                     && (e.MaxQuantity == 0 || e.MaxQuantity >= request.Quantity))
            .ToListAsync(cancellationToken);

        if (!applicablePriceEntries.Any())
        {
            // Fallback a prezzo base prodotto
            searchPath.Add("No price lists available, using product base price");
            _logger.LogWarning("No price lists found for product {ProductId}, using base price {BasePrice}", 
                product.Id, product.DefaultPrice ?? 0m);

            return new ProductPriceResultDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Codes?.FirstOrDefault()?.Code,
                FinalPrice = product.DefaultPrice ?? 0m,
                Currency = "EUR",
                AppliedMode = PriceApplicationMode.Automatic,
                IsManual = false,
                IsPriceListForced = false,
                SearchPath = searchPath
            };
        }

        // Ordina per precedenza
        IEnumerable<PriceListEntry> orderedEntries;

        if (request.BusinessPartyId.HasValue)
        {
            // Con BusinessParty: priorità ai listini assegnati al BusinessParty
            searchPath.Add($"BusinessParty {request.BusinessPartyId.Value} specified");
            
            orderedEntries = applicablePriceEntries
                .OrderByDescending(e => e.PriceList!.BusinessParties.Any(bp => bp.BusinessPartyId == request.BusinessPartyId.Value))
                .ThenByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }
        else
        {
            // Senza BusinessParty: solo listini generici (senza BusinessParty assegnati)
            searchPath.Add("No BusinessParty specified, using generic price lists only");
            
            orderedEntries = applicablePriceEntries
                .Where(e => !e.PriceList!.BusinessParties.Any())
                .OrderByDescending(e => e.PriceList!.Priority)
                .ThenByDescending(e => e.PriceList!.CreatedAt);
        }

        var selectedEntry = orderedEntries.FirstOrDefault();

        if (selectedEntry == null)
        {
            // Fallback a prezzo base prodotto
            searchPath.Add("No applicable price lists found, using product base price");
            _logger.LogWarning("No applicable price lists found for product {ProductId}, using base price", product.Id);

            return new ProductPriceResultDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Codes?.FirstOrDefault()?.Code,
                FinalPrice = product.DefaultPrice ?? 0m,
                Currency = "EUR",
                AppliedMode = PriceApplicationMode.Automatic,
                IsManual = false,
                IsPriceListForced = false,
                SearchPath = searchPath
            };
        }

        var basePrice = selectedEntry.Price;
        var finalPrice = basePrice;
        decimal? discountPercentage = null;

        // Applica sconto BusinessParty se presente
        if (request.BusinessPartyId.HasValue)
        {
            var businessPartyRelation = selectedEntry.PriceList!.BusinessParties
                .FirstOrDefault(bp => bp.BusinessPartyId == request.BusinessPartyId.Value 
                                   && !bp.IsDeleted 
                                   && bp.Status == PriceListBusinessPartyStatus.Active);

            if (businessPartyRelation?.GlobalDiscountPercentage.HasValue == true)
            {
                discountPercentage = businessPartyRelation.GlobalDiscountPercentage.Value;
                finalPrice = basePrice * (1 - discountPercentage.Value / 100m);
                searchPath.Add($"Applied BusinessParty discount: {discountPercentage.Value}%");
            }
        }

        searchPath.Add($"Selected price list: {selectedEntry.PriceList?.Name} (Priority: {selectedEntry.PriceList?.Priority})");

        // Costruisci lista listini disponibili per UI
        var availablePriceLists = applicablePriceEntries
            .Select(e => new AvailablePriceListDto
            {
                PriceListId = e.PriceListId,
                Name = e.PriceList?.Name ?? string.Empty,
                Priority = e.PriceList?.Priority ?? 0,
                Price = e.Price,
                IsAssignedToBusinessParty = request.BusinessPartyId.HasValue && 
                    e.PriceList!.BusinessParties.Any(bp => bp.BusinessPartyId == request.BusinessPartyId.Value),
                IsDefault = e.PriceList?.IsDefault ?? false
            })
            .OrderByDescending(pl => pl.Priority)
            .ToList();

        _logger.LogInformation("Applied automatic price from price list {PriceListId} for product {ProductId}: {Price}", 
            selectedEntry.PriceListId, product.Id, finalPrice);

        return new ProductPriceResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductCode = product.Codes?.FirstOrDefault()?.Code,
            FinalPrice = finalPrice,
            Currency = selectedEntry.Currency,
            AppliedMode = PriceApplicationMode.Automatic,
            AppliedPriceListId = selectedEntry.PriceListId,
            AppliedPriceListName = selectedEntry.PriceList?.Name,
            BasePriceFromPriceList = basePrice,
            AppliedDiscountPercentage = discountPercentage,
            PriceAfterDiscount = discountPercentage.HasValue ? finalPrice : null,
            IsManual = false,
            IsPriceListForced = false,
            AvailablePriceLists = availablePriceLists,
            SearchPath = searchPath
        };
    }

    /// <summary>
    /// Gestisce modalità HybridForcedWithOverrides: usa manuale se presente, altrimenti listino forzato.
    /// </summary>
    private async Task<ProductPriceResultDto> ApplyHybridPriceAsync(
        GetProductPriceRequestDto request,
        Product product,
        CancellationToken cancellationToken)
    {
        ProductPriceResultDto result;
        
        // Se ManualPrice presente → usa ApplyManualPriceAsync
        if (request.ManualPrice.HasValue && request.ManualPrice.Value > 0)
        {
            result = await ApplyManualPriceAsync(request, product, cancellationToken);
        }
        else
        {
            // Altrimenti → usa ApplyForcedPriceListAsync
            result = await ApplyForcedPriceListAsync(request, product, cancellationToken);
        }

        // Update mode to Hybrid
        return result with { AppliedMode = PriceApplicationMode.HybridForcedWithOverrides };
    }

    #endregion

    #region Phase 2C - Price List Duplication

    /// <summary>
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    public async Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(
        Guid sourcePriceListId,
        DuplicatePriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Recupera il listino sorgente
            var sourcePriceList = await _context.PriceLists
                .Include(pl => pl.ProductPrices)
                    .ThenInclude(pp => pp.Product)
                .Include(pl => pl.BusinessParties)
                    .ThenInclude(plbp => plbp.BusinessParty)
                .FirstOrDefaultAsync(pl => pl.Id == sourcePriceListId && !pl.IsDeleted, cancellationToken);

            if (sourcePriceList == null)
            {
                _logger.LogWarning("Source price list {PriceListId} not found for duplication", sourcePriceListId);
                throw new InvalidOperationException($"Price list {sourcePriceListId} not found");
            }

            // Count source entries BEFORE adding the new price list
            var sourceEntriesCount = sourcePriceList.ProductPrices?.Count(pp => !pp.IsDeleted) ?? 0;
            
            // If the navigation property is empty, do a direct query (can happen with in-memory DB in tests)
            if (sourceEntriesCount == 0)
            {
                sourceEntriesCount = await _context.PriceListEntries
                    .Where(e => e.PriceListId == sourcePriceListId && !e.IsDeleted)
                    .CountAsync(cancellationToken);
            }

            // 2. Genera codice se non fornito
            var newCode = dto.Code ?? await GenerateUniquePriceListCodeAsync(
                dto.Name, cancellationToken);

            // 3. Crea il nuovo listino (copia metadati)
            var newPriceList = new Data.Entities.PriceList.PriceList
            {
                Id = Guid.NewGuid(),
                TenantId = sourcePriceList.TenantId,
                Name = dto.Name,
                Description = dto.Description ?? $"Duplicato da: {sourcePriceList.Name}",
                Code = newCode,
                Type = dto.NewType ?? sourcePriceList.Type,
                Direction = dto.NewDirection ?? sourcePriceList.Direction,
                Status = (Data.Entities.PriceList.PriceListStatus)dto.NewStatus,
                Priority = dto.NewPriority ?? sourcePriceList.Priority,
                ValidFrom = dto.NewValidFrom ?? sourcePriceList.ValidFrom,
                ValidTo = dto.NewValidTo ?? sourcePriceList.ValidTo,
                EventId = dto.NewEventId ?? sourcePriceList.EventId,
                IsDefault = false, // Mai copiare IsDefault
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.PriceLists.Add(newPriceList);

            var stats = new
            {
                SourcePriceCount = sourceEntriesCount,
                CopiedPriceCount = 0,
                SkippedPriceCount = 0,
                CopiedBusinessPartyCount = 0
            };

            // 4. Copia le voci di prezzo (se richiesto)
            if (dto.CopyPrices)
            {
                var pricesToCopy = sourcePriceList.ProductPrices
                    .Where(pp => !pp.IsDeleted && pp.Status == PriceListEntryStatus.Active);

                // Applica filtri
                if (dto.OnlyActiveProducts)
                {
                    pricesToCopy = pricesToCopy.Where(pp => pp.Product != null && !pp.Product.IsDeleted);
                }

                if (dto.FilterByProductIds?.Any() == true)
                {
                    pricesToCopy = pricesToCopy.Where(pp =>
                        dto.FilterByProductIds.Contains(pp.ProductId));
                }

                if (dto.FilterByCategoryIds?.Any() == true)
                {
                    pricesToCopy = pricesToCopy.Where(pp =>
                        pp.Product.CategoryNodeId.HasValue &&
                        dto.FilterByCategoryIds.Contains(pp.Product.CategoryNodeId.Value));
                }

                var pricesList = pricesToCopy.ToList();

                foreach (var sourcePrice in pricesList)
                {
                    var newPrice = sourcePrice.Price;

                    // Applica maggiorazione se specificata
                    if (dto.ApplyMarkupPercentage.HasValue)
                    {
                        newPrice *= (1 + dto.ApplyMarkupPercentage.Value / 100);
                    }

                    // Applica arrotondamento se specificato
                    if (dto.RoundingStrategy.HasValue)
                    {
                        newPrice = ApplyRounding(newPrice, dto.RoundingStrategy.Value);
                    }

                    var newEntry = new PriceListEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = sourcePriceList.TenantId,
                        PriceListId = newPriceList.Id,
                        ProductId = sourcePrice.ProductId,
                        Price = newPrice,
                        Currency = sourcePrice.Currency,
                        MinQuantity = sourcePrice.MinQuantity,
                        MaxQuantity = sourcePrice.MaxQuantity,
                        LeadTimeDays = sourcePrice.LeadTimeDays,
                        MinimumOrderQuantity = sourcePrice.MinimumOrderQuantity,
                        SupplierProductCode = sourcePrice.SupplierProductCode,
                        IsEditableInFrontend = sourcePrice.IsEditableInFrontend,
                        IsDiscountable = sourcePrice.IsDiscountable,
                        Score = sourcePrice.Score,
                        Status = PriceListEntryStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };

                    _context.PriceListEntries.Add(newEntry);
                    stats = stats with { CopiedPriceCount = stats.CopiedPriceCount + 1 };
                }

                stats = stats with {
                    SkippedPriceCount = stats.SourcePriceCount - stats.CopiedPriceCount
                };
            }

            // 5. Copia le assegnazioni BusinessParty (se richiesto)
            if (dto.CopyBusinessParties)
            {
                foreach (var sourceBP in sourcePriceList.BusinessParties.Where(bp => !bp.IsDeleted))
                {
                    var newBP = new PriceListBusinessParty
                    {
                        Id = Guid.NewGuid(),
                        TenantId = sourcePriceList.TenantId,
                        PriceListId = newPriceList.Id,
                        BusinessPartyId = sourceBP.BusinessPartyId,
                        IsPrimary = sourceBP.IsPrimary,
                        OverridePriority = sourceBP.OverridePriority,
                        GlobalDiscountPercentage = sourceBP.GlobalDiscountPercentage,
                        SpecificValidFrom = sourceBP.SpecificValidFrom,
                        SpecificValidTo = sourceBP.SpecificValidTo,
                        Status = sourceBP.Status,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };

                    _context.PriceListBusinessParties.Add(newBP);
                    stats = stats with { CopiedBusinessPartyCount = stats.CopiedBusinessPartyCount + 1 };
                }
            }

            // 6. Salva tutto
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price list duplicated: {SourceId} -> {NewId} ({CopiedPrices} prices, {CopiedBP} business parties)",
                sourcePriceListId, newPriceList.Id, stats.CopiedPriceCount, stats.CopiedBusinessPartyCount);

            // 7. Recupera il listino completo per il DTO
            var newPriceListDto = await GetPriceListByIdAsync(newPriceList.Id, cancellationToken);

            return new DuplicatePriceListResultDto
            {
                SourcePriceListId = sourcePriceListId,
                SourcePriceListName = sourcePriceList.Name,
                NewPriceList = newPriceListDto!,
                SourcePriceCount = stats.SourcePriceCount,
                CopiedPriceCount = stats.CopiedPriceCount,
                SkippedPriceCount = stats.SkippedPriceCount,
                CopiedBusinessPartyCount = stats.CopiedBusinessPartyCount,
                AppliedMarkupPercentage = dto.ApplyMarkupPercentage,
                AppliedRoundingStrategy = dto.RoundingStrategy,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating price list {PriceListId}", sourcePriceListId);
            throw;
        }
    }

    /// <summary>
    /// Genera un codice univoco per il listino basato sul nome.
    /// </summary>
    private async Task<string> GenerateUniquePriceListCodeAsync(
        string name,
        CancellationToken cancellationToken)
    {
        // Normalizza il nome per creare un codice base usando System.Text per rimuovere accenti
        var normalized = name.Normalize(System.Text.NormalizationForm.FormD);
        var withoutAccents = new string(normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) 
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(System.Text.NormalizationForm.FormC);

        var baseCode = new string(withoutAccents
            .ToUpperInvariant()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .Take(20)
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = "PRICELIST";

        var code = baseCode;
        var counter = 1;

        while (await _context.PriceLists.AnyAsync(
            pl => pl.Code == code && !pl.IsDeleted,
            cancellationToken))
        {
            code = $"{baseCode}-{counter}";
            counter++;
        }

        return code;
    }

    /// <summary>
    /// Applica la strategia di arrotondamento al prezzo.
    /// </summary>
    private static decimal ApplyRounding(decimal value, EventForge.DTOs.Common.RoundingStrategy strategy)
    {
        return strategy switch
        {
            EventForge.DTOs.Common.RoundingStrategy.ToNearest5Cents =>
                Math.Round(value * 20, MidpointRounding.AwayFromZero) / 20m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearest10Cents =>
                Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearest50Cents =>
                Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2m,

            EventForge.DTOs.Common.RoundingStrategy.ToNearestEuro =>
                Math.Round(value, MidpointRounding.AwayFromZero),

            EventForge.DTOs.Common.RoundingStrategy.ToNearest99Cents =>
                Math.Floor(value) + 0.99m,

            _ => value
        };
    }

    /// <summary>
    /// Calcola il nuovo prezzo in base all'operazione e al valore.
    /// </summary>
    private static decimal CalculateNewPrice(decimal currentPrice, EventForge.DTOs.Common.BulkUpdateOperation operation, decimal value)
    {
        return operation switch
        {
            EventForge.DTOs.Common.BulkUpdateOperation.IncreaseByPercentage => currentPrice * (1 + value / 100),
            EventForge.DTOs.Common.BulkUpdateOperation.DecreaseByPercentage => currentPrice * (1 - value / 100),
            EventForge.DTOs.Common.BulkUpdateOperation.IncreaseByAmount => currentPrice + value,
            EventForge.DTOs.Common.BulkUpdateOperation.DecreaseByAmount => currentPrice - value,
            EventForge.DTOs.Common.BulkUpdateOperation.SetFixedPrice => value,
            EventForge.DTOs.Common.BulkUpdateOperation.MultiplyBy => currentPrice * value,
            _ => currentPrice
        };
    }

    #region Bulk Price Update Methods

    /// <summary>
    /// Anteprima aggiornamento massivo prezzi
    /// </summary>
    public async Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica esistenza listino
            var priceListExists = await _context.PriceLists
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (!priceListExists)
            {
                throw new InvalidOperationException($"Price list {priceListId} not found.");
            }

            // Query base per gli items del listino
            IQueryable<PriceListEntry> query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product);

            // Applica filtri
            query = ApplyBulkUpdateFilters(query, dto);

            // Recupera items
            var items = await query.ToListAsync(cancellationToken);

            var changes = new List<PriceChangePreview>();
            decimal totalCurrentValue = 0;
            decimal totalNewValue = 0;

            foreach (var item in items)
            {
                var currentPrice = item.Price;
                var newPrice = CalculateNewPrice(currentPrice, dto.Operation, dto.Value);
                newPrice = ApplyRounding(newPrice, dto.RoundingStrategy);

                // Assicura che il prezzo non sia negativo
                if (newPrice < 0)
                    newPrice = 0;

                var changeAmount = newPrice - currentPrice;
                var changePercentage = currentPrice != 0 
                    ? (changeAmount / currentPrice) * 100 
                    : 0;

                changes.Add(new PriceChangePreview
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Unknown",
                    ProductCode = item.Product?.Code,
                    CurrentPrice = currentPrice,
                    NewPrice = newPrice,
                    ChangeAmount = changeAmount,
                    ChangePercentage = changePercentage
                });

                totalCurrentValue += currentPrice;
                totalNewValue += newPrice;
            }

            var averageIncreasePercentage = totalCurrentValue != 0
                ? ((totalNewValue - totalCurrentValue) / totalCurrentValue) * 100
                : 0;

            return new BulkUpdatePreviewDto
            {
                AffectedCount = changes.Count,
                Changes = changes,
                TotalCurrentValue = totalCurrentValue,
                TotalNewValue = totalNewValue,
                AverageIncreasePercentage = averageIncreasePercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing bulk price update for price list {PriceListId}", priceListId);
            throw;
        }
    }

    /// <summary>
    /// Esegue aggiornamento massivo prezzi
    /// </summary>
    public async Task<BulkUpdateResultDto> BulkUpdatePricesAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica esistenza listino
            var priceListExists = await _context.PriceLists
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (!priceListExists)
            {
                throw new InvalidOperationException($"Price list {priceListId} not found.");
            }

            // Query base per gli items del listino
            IQueryable<PriceListEntry> query = _context.PriceListEntries
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .Include(ple => ple.Product);

            // Applica filtri
            query = ApplyBulkUpdateFilters(query, dto);

            // Recupera items
            var items = await query.ToListAsync(cancellationToken);

            var result = new BulkUpdateResultDto
            {
                UpdatedAt = DateTime.UtcNow,
                Errors = new List<string>()
            };

            // Aggiorna prezzi
            foreach (var item in items)
            {
                try
                {
                    var currentPrice = item.Price;
                    var newPrice = CalculateNewPrice(currentPrice, dto.Operation, dto.Value);
                    newPrice = ApplyRounding(newPrice, dto.RoundingStrategy);

                    // Assicura che il prezzo non sia negativo
                    if (newPrice < 0)
                    {
                        result.Errors.Add($"Product {item.Product?.Name ?? item.ProductId.ToString()}: Calculated price is negative, skipping.");
                        result.FailedCount++;
                        continue;
                    }

                    item.Price = newPrice;
                    item.ModifiedBy = currentUser;
                    item.ModifiedAt = DateTime.UtcNow;

                    result.UpdatedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Product {item.Product?.Name ?? item.ProductId.ToString()}: {ex.Message}");
                    result.FailedCount++;
                    _logger.LogError(ex, "Error updating price for product {ProductId} in price list {PriceListId}", 
                        item.ProductId, priceListId);
                }
            }

            // Salva modifiche in una transazione
            if (result.UpdatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);

                // Audit log per l'operazione bulk
                await _auditLogService.LogEntityChangeAsync(
                    "PriceList",
                    priceListId,
                    "BulkUpdate",
                    "BulkUpdate",
                    null,
                    $"Operation: {dto.Operation}, Value: {dto.Value}, Updated: {result.UpdatedCount}, Failed: {result.FailedCount}",
                    currentUser,
                    null,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk price update for price list {PriceListId}", priceListId);
            throw;
        }
    }

    /// <summary>
    /// Applica i filtri alla query per il bulk update.
    /// </summary>
    private IQueryable<PriceListEntry> ApplyBulkUpdateFilters(
        IQueryable<PriceListEntry> query,
        BulkPriceUpdateDto dto)
    {
        // Filtro per ProductIds specifici
        if (dto.ProductIds != null && dto.ProductIds.Any())
        {
            query = query.Where(ple => dto.ProductIds.Contains(ple.ProductId));
        }

        // Filtro per CategoryIds
        if (dto.CategoryIds != null && dto.CategoryIds.Any())
        {
            query = query.Where(ple => ple.Product != null && 
                dto.CategoryIds.Contains(ple.Product.CategoryNodeId!.Value));
        }

        // Filtro per BrandIds
        if (dto.BrandIds != null && dto.BrandIds.Any())
        {
            query = query.Where(ple => ple.Product != null && 
                ple.Product.BrandId != null &&
                dto.BrandIds.Contains(ple.Product.BrandId.Value));
        }

        // Filtro per MinPrice
        if (dto.MinPrice.HasValue)
        {
            query = query.Where(ple => ple.Price >= dto.MinPrice.Value);
        }

        // Filtro per MaxPrice
        if (dto.MaxPrice.HasValue)
        {
            query = query.Where(ple => ple.Price <= dto.MaxPrice.Value);
        }

        return query;
    }

    #endregion

    #region Phase 2C - PR #4: Price list generation from purchase documents

    /// <summary>
    /// Helper class per memorizzare occorrenze di prezzi nei documenti
    /// </summary>
    private class PriceOccurrence
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
        public Guid DocumentId { get; set; }
    }

    /// <summary>
    /// Preview generazione listino da documenti (senza salvataggio)
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validazione fornitore e recupero TenantId
        var supplier = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);
        
        if (supplier == null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        if (dto.ToDate > DateTime.UtcNow)
        {
            throw new InvalidOperationException("La data di fine non può essere nel futuro");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            dto.SupplierId,
            dto.FromDate,
            dto.ToDate,
            dto.FilterByCategoryIds,
            dto.OnlyActiveProducts,
            dto.MinimumQuantity,
            tenantId,
            cancellationToken);

        var productPreviews = new List<ProductPricePreview>();
        decimal totalValue = 0;

        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);
            var originalPrice = calculatedPrice;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
                continue;

            productPreviews.Add(new ProductPricePreview
            {
                ProductId = productId,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                CalculatedPrice = calculatedPrice,
                OriginalPrice = originalPrice,
                OccurrencesInDocuments = occurrences.Count,
                LowestPrice = occurrences.Min(o => o.Price),
                HighestPrice = occurrences.Max(o => o.Price),
                AveragePrice = occurrences.Average(o => o.Price),
                LastPurchaseDate = occurrences.Max(o => o.Date)
            });

            totalValue += calculatedPrice;
        }

        // Conta documenti distinti
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        return new GeneratePriceListPreviewDto
        {
            TotalDocumentsAnalyzed = documentCount,
            TotalProductsFound = productPricesDict.Count,
            ProductsWithMultiplePrices = productPricesDict.Count(kvp => kvp.Value.Count > 1),
            ProductPreviews = productPreviews,
            TotalEstimatedValue = totalValue,
            AnalysisFromDate = dto.FromDate,
            AnalysisToDate = dto.ToDate
        };
    }

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    public async Task<Guid> GenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Validazione fornitore e recupero TenantId
        var supplier = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == dto.SupplierId, cancellationToken);
        
        if (supplier == null)
        {
            throw new InvalidOperationException($"Fornitore {dto.SupplierId} non trovato");
        }

        var tenantId = supplier.TenantId;

        // Validazione range date
        if (dto.FromDate >= dto.ToDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        if (dto.ToDate > DateTime.UtcNow)
        {
            throw new InvalidOperationException("La data di fine non può essere nel futuro");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            dto.SupplierId,
            dto.FromDate,
            dto.ToDate,
            dto.FilterByCategoryIds,
            dto.OnlyActiveProducts,
            dto.MinimumQuantity,
            tenantId,
            cancellationToken);

        if (!productPricesDict.Any())
        {
            throw new InvalidOperationException("Nessun prodotto trovato nei documenti del periodo specificato");
        }

        // Crea PriceList
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Code = dto.Code ?? await GenerateUniquePriceListCodeAsync(tenantId, cancellationToken),
            Type = PriceListType.Purchase,
            Direction = PriceListDirection.Input,
            Status = PriceListStatus.Active,
            IsGeneratedFromDocuments = true,
            LastSyncedAt = DateTime.UtcNow,
            LastSyncedBy = currentUser,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        // Conta documenti per metadati
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == dto.SupplierId &&
                        dh.Date >= dto.FromDate &&
                        dh.Date <= dto.ToDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        // Salva metadati
        var metadata = new PriceListGenerationMetadata
        {
            Strategy = dto.CalculationStrategy,
            Rounding = dto.RoundingStrategy,
            MarkupPercentage = dto.MarkupPercentage,
            AnalysisFromDate = dto.FromDate,
            AnalysisToDate = dto.ToDate,
            DocumentsAnalyzed = documentCount,
            ProductsGenerated = productPricesDict.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = currentUser
        };

        priceList.GenerationMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);

        _context.PriceLists.Add(priceList);

        // Crea PriceListBusinessParty
        var priceListBusinessParty = new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = dto.SupplierId,
            Status = PriceListBusinessPartyStatus.Active,
            TenantId = tenantId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        _context.PriceListBusinessParties.Add(priceListBusinessParty);

        // Crea PriceListEntries
        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var entry = new PriceListEntry
            {
                Id = Guid.NewGuid(),
                PriceListId = priceList.Id,
                ProductId = productId,
                Price = calculatedPrice,
                Status = PriceListEntryStatus.Active,
                TenantId = tenantId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Create",
            "GenerateFromPurchases",
            null,
            $"Generated price list '{priceList.Name}' from {productPricesDict.Count} products in {documentCount} purchase documents",
            currentUser,
            null,
            cancellationToken);

        return priceList.Id;
    }

    /// <summary>
    /// Preview aggiornamento listino esistente
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        // Carica listino esistente
        var priceList = await _context.PriceLists
            .Include(pl => pl.BusinessParties)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation == null)
        {
            throw new InvalidOperationException("Listino non ha un fornitore assegnato");
        }

        var supplierId = supplierRelation.BusinessPartyId;

        // Default range date
        var fromDate = dto.FromDate ?? DateTime.UtcNow.AddDays(-90);
        var toDate = dto.ToDate ?? DateTime.UtcNow;

        // Validazione range date
        if (fromDate >= toDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            supplierId,
            fromDate,
            toDate,
            null,
            false,
            null,
            tenantId,
            cancellationToken);

        var productPreviews = new List<ProductPricePreview>();
        decimal totalValue = 0;

        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);
            var originalPrice = calculatedPrice;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
                continue;

            productPreviews.Add(new ProductPricePreview
            {
                ProductId = productId,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                CalculatedPrice = calculatedPrice,
                OriginalPrice = originalPrice,
                OccurrencesInDocuments = occurrences.Count,
                LowestPrice = occurrences.Min(o => o.Price),
                HighestPrice = occurrences.Max(o => o.Price),
                AveragePrice = occurrences.Average(o => o.Price),
                LastPurchaseDate = occurrences.Max(o => o.Date)
            });

            totalValue += calculatedPrice;
        }

        // Conta documenti distinti
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        return new GeneratePriceListPreviewDto
        {
            TotalDocumentsAnalyzed = documentCount,
            TotalProductsFound = productPricesDict.Count,
            ProductsWithMultiplePrices = productPricesDict.Count(kvp => kvp.Value.Count > 1),
            ProductPreviews = productPreviews,
            TotalEstimatedValue = totalValue,
            AnalysisFromDate = fromDate,
            AnalysisToDate = toDate
        };
    }

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    public async Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Carica listino esistente
        var priceList = await _context.PriceLists
            .Include(pl => pl.BusinessParties)
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        // Ottieni fornitore
        var supplierRelation = priceList.BusinessParties.FirstOrDefault();
        if (supplierRelation == null)
        {
            throw new InvalidOperationException("Listino non ha un fornitore assegnato");
        }

        var supplierId = supplierRelation.BusinessPartyId;

        // Default range date
        var fromDate = dto.FromDate ?? DateTime.UtcNow.AddDays(-90);
        var toDate = dto.ToDate ?? DateTime.UtcNow;

        // Validazione range date
        if (fromDate >= toDate)
        {
            throw new InvalidOperationException("La data di inizio deve essere precedente alla data di fine");
        }

        // Ottieni prezzi dai documenti
        var productPricesDict = await GetProductPricesFromDocumentsAsync(
            supplierId,
            fromDate,
            toDate,
            null,
            false,
            null,
            tenantId,
            cancellationToken);

        int pricesUpdated = 0;
        int pricesAdded = 0;
        int pricesRemoved = 0;
        int pricesUnchanged = 0;
        var warnings = new List<string>();

        // Calcola nuovi prezzi
        var newPrices = new Dictionary<Guid, decimal>();
        foreach (var kvp in productPricesDict)
        {
            var productId = kvp.Key;
            var occurrences = kvp.Value;

            if (!occurrences.Any())
                continue;

            var calculatedPrice = CalculatePriceByStrategy(occurrences, dto.CalculationStrategy);

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                calculatedPrice *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            calculatedPrice = ApplyRounding(calculatedPrice, dto.RoundingStrategy);

            newPrices[productId] = calculatedPrice;
        }

        // Aggiorna prezzi esistenti
        foreach (var entry in priceList.ProductPrices.ToList())
        {
            if (newPrices.TryGetValue(entry.ProductId, out var newPrice))
            {
                if (Math.Abs(entry.Price - newPrice) > 0.001m)
                {
                    entry.Price = newPrice;
                    entry.ModifiedBy = currentUser;
                    entry.ModifiedAt = DateTime.UtcNow;
                    pricesUpdated++;
                }
                else
                {
                    pricesUnchanged++;
                }

                newPrices.Remove(entry.ProductId);
            }
            else if (dto.RemoveObsoleteProducts)
            {
                _context.PriceListEntries.Remove(entry);
                pricesRemoved++;
            }
            else
            {
                pricesUnchanged++;
            }
        }

        // Aggiungi nuovi prodotti
        if (dto.AddNewProducts)
        {
            foreach (var kvp in newPrices)
            {
                var entry = new PriceListEntry
                {
                    Id = Guid.NewGuid(),
                    PriceListId = priceList.Id,
                    ProductId = kvp.Key,
                    Price = kvp.Value,
                    Status = PriceListEntryStatus.Active,
                    TenantId = tenantId,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                _context.PriceListEntries.Add(entry);
                pricesAdded++;
            }
        }
        else if (newPrices.Any())
        {
            warnings.Add($"{newPrices.Count} nuovi prodotti trovati ma non aggiunti (AddNewProducts = false)");
        }

        // Conta documenti per metadati
        var documentCount = await _context.DocumentHeaders
            .Where(dh => dh.TenantId == tenantId &&
                        dh.BusinessPartyId == supplierId &&
                        dh.Date >= fromDate &&
                        dh.Date <= toDate &&
                        dh.DocumentType!.IsStockIncrease)
            .CountAsync(cancellationToken);

        // Aggiorna metadati listino
        var metadata = new PriceListGenerationMetadata
        {
            Strategy = dto.CalculationStrategy,
            Rounding = dto.RoundingStrategy,
            MarkupPercentage = dto.MarkupPercentage,
            AnalysisFromDate = fromDate,
            AnalysisToDate = toDate,
            DocumentsAnalyzed = documentCount,
            ProductsGenerated = productPricesDict.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = currentUser
        };

        priceList.GenerationMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);
        priceList.LastSyncedAt = DateTime.UtcNow;
        priceList.LastSyncedBy = currentUser;
        priceList.IsGeneratedFromDocuments = true;
        priceList.ModifiedBy = currentUser;
        priceList.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Update",
            "UpdateFromPurchases",
            null,
            $"Updated: {pricesUpdated}, Added: {pricesAdded}, Removed: {pricesRemoved}, Unchanged: {pricesUnchanged}",
            currentUser,
            null,
            cancellationToken);

        return new UpdatePriceListResultDto
        {
            PriceListId = priceList.Id,
            PriceListName = priceList.Name,
            PricesUpdated = pricesUpdated,
            PricesAdded = pricesAdded,
            PricesRemoved = pricesRemoved,
            PricesUnchanged = pricesUnchanged,
            SyncedAt = DateTime.UtcNow,
            SyncedBy = currentUser,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Ottiene prezzi da documenti di carico per un fornitore
    /// </summary>
    private async Task<Dictionary<Guid, List<PriceOccurrence>>> GetProductPricesFromDocumentsAsync(
        Guid supplierId,
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? filterByCategoryIds,
        bool onlyActiveProducts,
        decimal? minimumQuantity,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Query documenti di carico (IsStockIncrease = true)
        var query = _context.DocumentRows
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Include(dr => dr.Product)
            .Where(dr => dr.TenantId == tenantId &&
                        dr.DocumentHeader!.BusinessPartyId == supplierId &&
                        dr.DocumentHeader.Date >= fromDate &&
                        dr.DocumentHeader.Date <= toDate &&
                        dr.DocumentHeader.DocumentType!.IsStockIncrease &&
                        dr.ProductId != null &&
                        dr.UnitPrice > 0);

        // Filtri opzionali
        if (onlyActiveProducts)
        {
            query = query.Where(dr => dr.Product!.Status == Data.Entities.Products.ProductStatus.Active);
        }

        if (filterByCategoryIds != null && filterByCategoryIds.Any())
        {
            query = query.Where(dr => dr.Product!.CategoryNodeId != null &&
                                    filterByCategoryIds.Contains(dr.Product.CategoryNodeId.Value));
        }

        var documentRows = await query.ToListAsync(cancellationToken);

        // Raggruppa per prodotto
        var productPrices = documentRows
            .Where(dr => dr.ProductId.HasValue)
            .GroupBy(dr => dr.ProductId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(dr => new PriceOccurrence
                {
                    Price = dr.UnitPrice,
                    Quantity = dr.Quantity,
                    Date = dr.DocumentHeader?.Date ?? DateTime.UtcNow,
                    DocumentId = dr.DocumentHeaderId
                }).ToList()
            );

        // Filtra per quantità minima
        if (minimumQuantity.HasValue)
        {
            productPrices = productPrices
                .Where(kvp => kvp.Value.Sum(o => o.Quantity) >= minimumQuantity.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return productPrices;
    }

    /// <summary>
    /// Calcola il prezzo secondo la strategia specificata
    /// </summary>
    private decimal CalculatePriceByStrategy(
        List<PriceOccurrence> occurrences,
        PriceCalculationStrategy strategy)
    {
        if (occurrences == null || !occurrences.Any())
            throw new InvalidOperationException("Nessun prezzo disponibile per il calcolo");

        return strategy switch
        {
            PriceCalculationStrategy.LastPurchasePrice =>
                occurrences.OrderByDescending(p => p.Date).First().Price,

            PriceCalculationStrategy.WeightedAveragePrice =>
                occurrences.Sum(p => p.Price * p.Quantity) / occurrences.Sum(p => p.Quantity),

            PriceCalculationStrategy.SimpleAveragePrice =>
                occurrences.Average(p => p.Price),

            PriceCalculationStrategy.LowestPrice =>
                occurrences.Min(p => p.Price),

            PriceCalculationStrategy.HighestPrice =>
                occurrences.Max(p => p.Price),

            PriceCalculationStrategy.MedianPrice =>
                CalculateMedian(occurrences.Select(p => p.Price).ToList()),

            _ => throw new ArgumentException($"Strategia non supportata: {strategy}")
        };
    }

    /// <summary>
    /// Calcola la mediana di una lista di valori
    /// </summary>
    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values == null || !values.Any())
            throw new ArgumentException("Lista valori vuota");

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    /// <summary>
    /// Genera un codice univoco per il listino
    /// </summary>
    private async Task<string> GenerateUniquePriceListCodeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var baseCode = $"PL-{DateTime.UtcNow:yyyyMMdd}";
        var code = baseCode;
        var counter = 1;

        while (await _context.PriceLists.AnyAsync(pl => pl.TenantId == tenantId && pl.Code == code, cancellationToken))
        {
            code = $"{baseCode}-{counter:D3}";
            counter++;
        }

        return code;
    }

    #endregion

    #region Price List Generation from Products

    /// <summary>
    /// Genera nuovo listino dai prezzi DefaultPrice dei prodotti
    /// </summary>
    public async Task<Guid> GenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Validazione e recupero TenantId da un prodotto esistente
        // Prima troviamo almeno un prodotto per ottenere il TenantId
        var anyProduct = await _context.Products
            .Where(p => !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (anyProduct == null)
        {
            throw new InvalidOperationException("Nessun prodotto disponibile nel sistema");
        }

        var tenantId = anyProduct.TenantId;

        // 2. Validazione EventId se specificato
        if (dto.EventId.HasValue)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.Id == dto.EventId.Value && e.TenantId == tenantId && !e.IsDeleted, cancellationToken);
            
            if (!eventExists)
            {
                throw new InvalidOperationException($"Evento {dto.EventId.Value} non trovato");
            }
        }

        // 3. Query prodotti con filtri
        var query = _context.Products
            .Where(p => p.TenantId == tenantId && !p.IsDeleted);

        // Filtro prodotti attivi
        if (dto.OnlyActiveProducts)
        {
            query = query.Where(p => p.Status == EventForge.Server.Data.Entities.Products.ProductStatus.Active);
        }

        // Filtro prodotti con prezzo
        if (dto.OnlyProductsWithPrice)
        {
            query = query.Where(p => p.DefaultPrice.HasValue && p.DefaultPrice.Value > 0);
        }

        // Filtro per categorie
        if (dto.FilterByCategoryIds != null && dto.FilterByCategoryIds.Any())
        {
            query = query.Where(p => p.CategoryNodeId.HasValue && dto.FilterByCategoryIds.Contains(p.CategoryNodeId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        if (!products.Any())
        {
            throw new InvalidOperationException("Nessun prodotto trovato con i criteri specificati");
        }

        if (dto.OnlyProductsWithPrice && !products.Any(p => p.DefaultPrice.HasValue && p.DefaultPrice.Value > 0))
        {
            throw new InvalidOperationException("Nessun prodotto trovato con prezzo maggiore di 0");
        }

        // 4. Crea PriceList
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Code = dto.Code ?? await GenerateUniquePriceListCodeAsync(tenantId, cancellationToken),
            Type = dto.Type,
            Direction = dto.Direction,
            Status = PriceListStatus.Active,
            EventId = dto.EventId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        _context.PriceLists.Add(priceList);

        // 5. Crea PriceListEntries
        var entriesCount = 0;
        foreach (var product in products)
        {
            if (!product.DefaultPrice.HasValue || product.DefaultPrice.Value <= 0)
                continue;

            var price = product.DefaultPrice.Value;

            // Applica maggiorazione
            if (dto.MarkupPercentage.HasValue)
            {
                price *= (1 + dto.MarkupPercentage.Value / 100);
            }

            // Applica arrotondamento
            price = ApplyRounding(price, dto.RoundingStrategy);

            var entry = new PriceListEntry
            {
                Id = Guid.NewGuid(),
                PriceListId = priceList.Id,
                ProductId = product.Id,
                Price = price,
                Status = PriceListEntryStatus.Active,
                TenantId = tenantId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
            entriesCount++;
        }

        // 6. Associa BusinessParties se specificati
        if (dto.BusinessPartyIds != null && dto.BusinessPartyIds.Any())
        {
            foreach (var businessPartyId in dto.BusinessPartyIds)
            {
                // Verifica che il BusinessParty esista
                var businessPartyExists = await _context.BusinessParties
                    .AnyAsync(bp => bp.Id == businessPartyId && bp.TenantId == tenantId && !bp.IsDeleted, cancellationToken);

                if (!businessPartyExists)
                {
                    _logger.LogWarning("BusinessParty {BusinessPartyId} non trovato, skip associazione", businessPartyId);
                    continue;
                }

                var priceListBusinessParty = new PriceListBusinessParty
                {
                    PriceListId = priceList.Id,
                    BusinessPartyId = businessPartyId,
                    Status = PriceListBusinessPartyStatus.Active,
                    TenantId = tenantId,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                _context.PriceListBusinessParties.Add(priceListBusinessParty);
            }
        }

        // 7. Salva e audit log
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Create",
            "GenerateFromProductPrices",
            null,
            $"Generated price list '{priceList.Name}' from {entriesCount} products",
            currentUser,
            null,
            cancellationToken);

        return priceList.Id;
    }

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice
    /// </summary>
    public async Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(
        ApplyPriceListToProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // 1. Validazione
        if (dto.OnlyUpdateIfHigher && dto.OnlyUpdateIfLower)
        {
            throw new InvalidOperationException("OnlyUpdateIfHigher e OnlyUpdateIfLower non possono essere entrambi true");
        }

        // 2. Carica listino con entries per ottenere TenantId
        var priceList = await _context.PriceLists
            .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
            .FirstOrDefaultAsync(pl => pl.Id == dto.PriceListId && !pl.IsDeleted, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException($"Listino {dto.PriceListId} non trovato");
        }

        var tenantId = priceList.TenantId;

        var result = new ApplyPriceListResultDto
        {
            PriceListId = priceList.Id,
            PriceListName = priceList.Name,
            AppliedAt = DateTime.UtcNow,
            AppliedBy = currentUser,
            UpdateDetails = new List<ProductPriceUpdateDetail>()
        };

        var updatedCount = 0;
        var skippedCount = 0;
        var notFoundCount = 0;

        // 3. Per ogni entry del listino
        foreach (var entry in priceList.ProductPrices)
        {
            if (entry.Status != PriceListEntryStatus.Active)
                continue;

            // Applica filtri se specificati
            if (dto.FilterByProductIds != null && dto.FilterByProductIds.Any() && 
                !dto.FilterByProductIds.Contains(entry.ProductId))
            {
                continue;
            }

            // Carica prodotto
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == entry.ProductId && p.TenantId == tenantId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                notFoundCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = entry.ProductId,
                    ProductName = "Unknown",
                    ProductCode = "Unknown",
                    OldPrice = 0,
                    NewPrice = entry.Price,
                    UpdateReason = "Not Found"
                });
                continue;
            }

            // Applica filtro categorie
            if (dto.FilterByCategoryIds != null && dto.FilterByCategoryIds.Any() &&
                (!product.CategoryNodeId.HasValue || !dto.FilterByCategoryIds.Contains(product.CategoryNodeId.Value)))
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = product.DefaultPrice ?? 0,
                    NewPrice = entry.Price,
                    UpdateReason = "Skipped - Category Filter"
                });
                continue;
            }

            var oldPrice = product.DefaultPrice ?? 0;
            var newPrice = entry.Price;

            // Verifica condizioni OnlyUpdateIfHigher/Lower
            if (dto.OnlyUpdateIfHigher && newPrice <= oldPrice)
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    UpdateReason = "Skipped - Not Higher"
                });
                continue;
            }

            if (dto.OnlyUpdateIfLower && newPrice >= oldPrice)
            {
                skippedCount++;
                result.UpdateDetails.Add(new ProductPriceUpdateDetail
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    UpdateReason = "Skipped - Not Lower"
                });
                continue;
            }

            // Backup prezzo se richiesto
            if (dto.CreateBackup)
            {
                await _auditLogService.LogEntityChangeAsync(
                    "Product",
                    product.Id,
                    "DefaultPrice",
                    "ApplyPriceList",
                    oldPrice.ToString("F2"),
                    newPrice.ToString("F2"),
                    currentUser,
                    product.Name,
                    cancellationToken);
            }

            // Aggiorna Product.DefaultPrice
            product.DefaultPrice = newPrice;
            // Non aggiorniamo ModifiedAt e ModifiedBy come da requisiti

            updatedCount++;
            result.UpdateDetails.Add(new ProductPriceUpdateDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductCode = product.Code ?? string.Empty,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                UpdateReason = "Updated"
            });
        }

        // 5. Salva modifiche
        await _context.SaveChangesAsync(cancellationToken);

        // Aggiorna risultato con i contatori finali
        var finalResult = new ApplyPriceListResultDto
        {
            PriceListId = result.PriceListId,
            PriceListName = result.PriceListName,
            ProductsUpdated = updatedCount,
            ProductsSkipped = skippedCount,
            ProductsNotFound = notFoundCount,
            UpdateDetails = result.UpdateDetails,
            AppliedAt = result.AppliedAt,
            AppliedBy = result.AppliedBy
        };

        // Log applicazione listino
        await _auditLogService.LogEntityChangeAsync(
            "PriceList",
            priceList.Id,
            "Action",
            "ApplyToProducts",
            null,
            $"Applied to {updatedCount} products, skipped {skippedCount}, not found {notFoundCount}",
            currentUser,
            priceList.Name,
            cancellationToken);

        return finalResult;
    }

    #endregion

    #endregion
}