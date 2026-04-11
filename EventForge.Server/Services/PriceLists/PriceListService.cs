using EventForge.DTOs.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using PriceListDirection = EventForge.DTOs.Common.PriceListDirection;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;

namespace EventForge.Server.Services.PriceLists;

public class PriceListService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<PriceListService> logger,
    IUnitConversionService unitConversionService,
    IPriceListGenerationService generationService,
    IPriceCalculationService calculationService,
    IPriceListBusinessPartyService businessPartyService,
    IPriceListBulkOperationsService bulkOperationsService) : IPriceListService
{
    private readonly IUnitConversionService _unitConversionService = unitConversionService;

    public async Task<PagedResult<PriceListDto>> GetPriceListsAsync(PaginationParameters pagination, PriceListDirection? direction = null, DTOs.Common.PriceListStatus? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.PriceLists
                .Where(pl => !pl.IsDeleted);

            // Apply filters BEFORE pagination
            if (direction.HasValue)
            {
                query = query.Where(pl => pl.Direction == direction.Value);
                logger.LogDebug("Filtering price lists by direction: {Direction}", direction.Value);
            }

            if (status.HasValue)
            {
                // Map DTO enum to entity enum by name (the two enums have different ordinal values)
                var entityStatus = status.Value switch
                {
                    DTOs.Common.PriceListStatus.Active => PriceListStatus.Active,
                    DTOs.Common.PriceListStatus.Suspended => PriceListStatus.Suspended,
                    DTOs.Common.PriceListStatus.Deleted => PriceListStatus.Deleted,
                    _ => (PriceListStatus?)null
                };
                if (entityStatus.HasValue)
                {
                    query = query.Where(pl => pl.Status == entityStatus.Value);
                }
                logger.LogDebug("Filtering price lists by status: {Status}", status.Value);
            }

            // Count AFTER filters
            var totalCount = await query.CountAsync(cancellationToken);

            logger.LogInformation(
                "Found {TotalCount} price lists matching filters (Direction: {Direction}, Status: {Status})",
                totalCount,
                direction?.ToString() ?? "Any",
                status?.ToString() ?? "Any");

            // Include related data and apply pagination AFTER filters
            var priceLists = await query
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .OrderBy(pl => pl.Priority)
                .ThenBy(pl => pl.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var priceListDtos = priceLists.Select(MapToPriceListDto);

            return new PagedResult<PriceListDto>
            {
                Items = priceListDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceLists = await context.PriceLists
                .AsNoTracking()
                .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .OrderBy(pl => pl.Priority)
                .ThenBy(pl => pl.Name)
                .ToListAsync(cancellationToken);

            return priceLists.Select(MapToPriceListDto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PriceListDto?> GetPriceListByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = await context.PriceLists
                .AsNoTracking()
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            return priceList is not null ? MapToPriceListDto(priceList) : null;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = await context.PriceLists
                .AsNoTracking()
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            return priceList is not null ? MapToPriceListDetailDto(priceList) : null;
        }
        catch (Exception ex)
        {
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

            _ = context.PriceLists.Add(priceList);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(priceList, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Price list created with ID {PriceListId} by user {User}.", priceList.Id, currentUser);

            return MapToPriceListDto(priceList);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PriceListDto?> UpdatePriceListAsync(Guid id, UpdatePriceListDto updatePriceListDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePriceListDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPriceList = await context.PriceLists
                .AsNoTracking()
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (originalPriceList is null)
            {
                logger.LogWarning("Price list with ID {PriceListId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var priceList = await context.PriceLists
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (priceList is null)
            {
                logger.LogWarning("Price list with ID {PriceListId} not found for update by user {User}.", id, currentUser);
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

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating PriceList {PriceListId}.", id);
                throw new InvalidOperationException("Il listino prezzi è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(priceList, "Update", currentUser, originalPriceList, cancellationToken);

            logger.LogInformation("Price list {PriceListId} updated by user {User}.", id, currentUser);

            return MapToPriceListDto(priceList);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeletePriceListAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPriceList = await context.PriceLists
                .AsNoTracking()
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (originalPriceList is null)
            {
                logger.LogWarning("Price list with ID {PriceListId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var priceList = await context.PriceLists
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(pl => pl.Id == id && !pl.IsDeleted, cancellationToken);

            if (priceList is null)
            {
                logger.LogWarning("Price list with ID {PriceListId} not found for deletion by user {User}.", id, currentUser);
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

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting PriceList {PriceListId}.", id);
                throw new InvalidOperationException("Il listino prezzi è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(priceList, "Delete", currentUser, originalPriceList, cancellationToken);

            logger.LogInformation("Price list {PriceListId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListEntryDto>> GetPriceListEntriesAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await context.PriceListEntries
                .AsNoTracking()
                .Include(ple => ple.UnitOfMeasure)
                .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
                .OrderBy(ple => ple.ProductId)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToPriceListEntryDto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PriceListEntryDto?> GetPriceListEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await context.PriceListEntries
                .AsNoTracking()
                .Include(ple => ple.UnitOfMeasure)
                .Where(ple => ple.Id == id && !ple.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return entry is not null ? MapToPriceListEntryDto(entry) : null;
        }
        catch (Exception ex)
        {
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
                UnitOfMeasureId = createPriceListEntryDto.UnitOfMeasureId,
                Notes = createPriceListEntryDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = context.PriceListEntries.Add(entry);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entry, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Price list entry created with ID {EntryId} for price list {PriceListId} by user {User}.",
                entry.Id, createPriceListEntryDto.PriceListId, currentUser);

            return MapToPriceListEntryDto(entry);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PriceListEntryDto?> UpdatePriceListEntryAsync(Guid id, UpdatePriceListEntryDto updatePriceListEntryDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePriceListEntryDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntry = await context.PriceListEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (originalEntry is null)
            {
                logger.LogWarning("Price list entry with ID {EntryId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entry = await context.PriceListEntries
                .Include(ple => ple.UnitOfMeasure)
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (entry is null)
            {
                logger.LogWarning("Price list entry with ID {EntryId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            entry.Price = updatePriceListEntryDto.Price;
            entry.Currency = updatePriceListEntryDto.Currency;
            entry.Score = updatePriceListEntryDto.Score;
            entry.IsEditableInFrontend = updatePriceListEntryDto.IsEditableInFrontend;
            entry.IsDiscountable = updatePriceListEntryDto.IsDiscountable;
            entry.MinQuantity = updatePriceListEntryDto.MinQuantity;
            entry.MaxQuantity = updatePriceListEntryDto.MaxQuantity;
            entry.UnitOfMeasureId = updatePriceListEntryDto.UnitOfMeasureId;
            entry.Notes = updatePriceListEntryDto.Notes;
            entry.ModifiedBy = currentUser;
            entry.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating PriceListEntry {PriceListEntryId}.", id);
                throw new InvalidOperationException("La voce del listino è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entry, "Update", currentUser, originalEntry, cancellationToken);

            logger.LogInformation("Price list entry {EntryId} updated by user {User}.", id, currentUser);

            return MapToPriceListEntryDto(entry);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> RemovePriceListEntryAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntry = await context.PriceListEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (originalEntry is null)
            {
                logger.LogWarning("Price list entry with ID {EntryId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entry = await context.PriceListEntries
                .FirstOrDefaultAsync(ple => ple.Id == id && !ple.IsDeleted, cancellationToken);

            if (entry is null)
            {
                logger.LogWarning("Price list entry with ID {EntryId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entry.IsDeleted = true;
            entry.DeletedBy = currentUser;
            entry.DeletedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting PriceListEntry {PriceListEntryId}.", id);
                throw new InvalidOperationException("La voce del listino è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entry, "Delete", currentUser, originalEntry, cancellationToken);

            logger.LogInformation("Price list entry {EntryId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> PriceListExistsAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.PriceLists
                .AsNoTracking()
                .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
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
            Status = priceList.Status switch
            {
                PriceListStatus.Active => EventForge.DTOs.Common.PriceListStatus.Active,
                PriceListStatus.Suspended => EventForge.DTOs.Common.PriceListStatus.Suspended,
                PriceListStatus.Deleted => EventForge.DTOs.Common.PriceListStatus.Deleted,
                _ => EventForge.DTOs.Common.PriceListStatus.Active
            },
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
            UnitOfMeasureId = entry.UnitOfMeasureId,
            UnitOfMeasureName = entry.UnitOfMeasure?.Name,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            CreatedBy = entry.CreatedBy,
            ModifiedAt = entry.ModifiedAt,
            ModifiedBy = entry.ModifiedBy
        };
    }

    #region Delegated to Calculation Service

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
            return await calculationService.GetAppliedPriceAsync(productId, eventId, businessPartyId, evaluationDate, quantity, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            return await calculationService.GetAppliedPriceWithUnitConversionAsync(productId, eventId, targetUnitId, evaluationDate, quantity, null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await calculationService.GetPriceHistoryAsync(productId, eventId, fromDate, toDate, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(
        Guid productId,
        int quantity = 1,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await calculationService.GetPurchasePriceComparisonAsync(productId, quantity, evaluationDate, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// </summary>
    public async Task<ProductPriceResultDto> GetProductPriceAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await calculationService.GetProductPriceAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Delegated to Bulk Operations Service

    public async Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return await bulkOperationsService.BulkImportPriceListEntriesAsync(priceListId, entries, currentUser, replaceExisting, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return await bulkOperationsService.ExportPriceListEntriesAsync(priceListId, includeInactiveEntries, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await bulkOperationsService.ValidatePriceListPrecedenceAsync(eventId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Delegated to BusinessParty Service

    public async Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await businessPartyService.AssignBusinessPartyAsync(priceListId, dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await businessPartyService.RemoveBusinessPartyAsync(priceListId, businessPartyId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await businessPartyService.GetBusinessPartiesForPriceListAsync(priceListId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByTypeAsync(PriceListType type, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceLists = await context.PriceLists
                .AsNoTracking()
                .Where(pl => pl.Type == type && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .OrderBy(pl => pl.Priority)
                .ThenBy(pl => pl.Name)
                .ToListAsync(cancellationToken);

            return priceLists.Select(MapToPriceListDto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default)
    {
        try
        {
            return await businessPartyService.GetPriceListsByBusinessPartyAsync(businessPartyId, type, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Delegated to Generation Service

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
            return await generationService.DuplicatePriceListAsync(sourcePriceListId, dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
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
            return await bulkOperationsService.PreviewBulkUpdateAsync(priceListId, dto, cancellationToken);
        }
        catch (Exception ex)
        {
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
            return await bulkOperationsService.BulkUpdatePricesAsync(priceListId, dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Phase 2C - PR #4: Price list generation from purchase documents

    /// <summary>
    /// Preview generazione listino da documenti (senza salvataggio)
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await generationService.PreviewGenerateFromPurchasesAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    public async Task<Guid> GenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await generationService.GenerateFromPurchasesAsync(dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Preview aggiornamento listino esistente
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await generationService.PreviewUpdateFromPurchasesAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    public async Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await generationService.UpdateFromPurchasesAsync(dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
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
        try
        {
            return await generationService.GenerateFromProductPricesAsync(dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Preview generazione listino da prezzi default prodotti
    /// </summary>
    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await generationService.PreviewGenerateFromProductPricesAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice
    /// </summary>
    public async Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(
        ApplyPriceListToProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await bulkOperationsService.ApplyPriceListToProductsAsync(dto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #endregion

}
