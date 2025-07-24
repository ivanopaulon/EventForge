using EventForge.Models.PriceLists;
using EventForge.Data.Entities.PriceList;
using EventForge.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EventForge.Services.Audit;

namespace EventForge.Services.PriceLists;

/// <summary>
/// Service implementation for managing price lists and price list entries.
/// </summary>
public class PriceListService : IPriceListService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PriceListService> _logger;

    public PriceListService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<PriceListService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // PriceList CRUD operations

    public async Task<PagedResult<PriceListDto>> GetPriceListsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var priceLists = await _context.PriceLists
            .Where(pl => pl.EventId == eventId && !pl.IsDeleted)
            .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
            .OrderBy(pl => pl.Priority)
            .ThenBy(pl => pl.Name)
            .ToListAsync(cancellationToken);

        return priceLists.Select(MapToPriceListDto);
    }

    public async Task<PriceListDto?> GetPriceListByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var priceList = await _context.PriceLists
            .Where(pl => pl.Id == id && !pl.IsDeleted)
            .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return priceList != null ? MapToPriceListDto(priceList) : null;
    }

    public async Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var priceList = await _context.PriceLists
            .Where(pl => pl.Id == id && !pl.IsDeleted)
            .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return priceList != null ? MapToPriceListDetailDto(priceList) : null;
    }

    public async Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto createPriceListDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPriceListDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Check if event exists
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
                Status = createPriceListDto.Status,
                IsDefault = createPriceListDto.IsDefault,
                Priority = createPriceListDto.Priority,
                EventId = createPriceListDto.EventId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.PriceLists.Add(priceList);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created price list
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

            var priceList = await _context.PriceLists
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (priceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Store original for audit
            var originalPriceList = new Data.Entities.PriceList.PriceList
            {
                Id = priceList.Id,
                Name = priceList.Name,
                Description = priceList.Description,
                ValidFrom = priceList.ValidFrom,
                ValidTo = priceList.ValidTo,
                Notes = priceList.Notes,
                Status = priceList.Status,
                IsDefault = priceList.IsDefault,
                Priority = priceList.Priority,
                EventId = priceList.EventId,
                CreatedBy = priceList.CreatedBy,
                CreatedAt = priceList.CreatedAt,
                ModifiedBy = priceList.ModifiedBy,
                ModifiedAt = priceList.ModifiedAt
            };

            // Update properties
            priceList.Name = updatePriceListDto.Name;
            priceList.Description = updatePriceListDto.Description;
            priceList.ValidFrom = updatePriceListDto.ValidFrom;
            priceList.ValidTo = updatePriceListDto.ValidTo;
            priceList.Notes = updatePriceListDto.Notes;
            priceList.Status = updatePriceListDto.Status;
            priceList.IsDefault = updatePriceListDto.IsDefault;
            priceList.Priority = updatePriceListDto.Priority;
            priceList.ModifiedBy = currentUser;
            priceList.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated price list
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

            var priceList = await _context.PriceLists
                .Where(pl => pl.Id == id && !pl.IsDeleted)
                .Include(pl => pl.ProductPrices.Where(ple => !ple.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (priceList == null)
            {
                _logger.LogWarning("Price list with ID {PriceListId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalPriceList = new Data.Entities.PriceList.PriceList
            {
                Id = priceList.Id,
                Name = priceList.Name,
                Description = priceList.Description,
                ValidFrom = priceList.ValidFrom,
                ValidTo = priceList.ValidTo,
                Notes = priceList.Notes,
                Status = priceList.Status,
                IsDefault = priceList.IsDefault,
                Priority = priceList.Priority,
                EventId = priceList.EventId,
                CreatedBy = priceList.CreatedBy,
                CreatedAt = priceList.CreatedAt,
                ModifiedBy = priceList.ModifiedBy,
                ModifiedAt = priceList.ModifiedAt,
                IsDeleted = priceList.IsDeleted,
                DeletedBy = priceList.DeletedBy,
                DeletedAt = priceList.DeletedAt
            };

            // Soft delete the price list and all related entries
            priceList.IsDeleted = true;
            priceList.DeletedBy = currentUser;
            priceList.DeletedAt = DateTime.UtcNow;

            // Soft delete related entries
            foreach (var entry in priceList.ProductPrices.Where(ple => !ple.IsDeleted))
            {
                entry.IsDeleted = true;
                entry.DeletedBy = currentUser;
                entry.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted price list
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

    // PriceListEntry management operations

    public async Task<IEnumerable<PriceListEntryDto>> GetPriceListEntriesAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        var entries = await _context.PriceListEntries
            .Where(ple => ple.PriceListId == priceListId && !ple.IsDeleted)
            .OrderBy(ple => ple.ProductId)
            .ToListAsync(cancellationToken);

        return entries.Select(MapToPriceListEntryDto);
    }

    public async Task<PriceListEntryDto?> GetPriceListEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.PriceListEntries
            .Where(ple => ple.Id == id && !ple.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return entry != null ? MapToPriceListEntryDto(entry) : null;
    }

    public async Task<PriceListEntryDto> AddPriceListEntryAsync(CreatePriceListEntryDto createPriceListEntryDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPriceListEntryDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Check if price list exists
            if (!await PriceListExistsAsync(createPriceListEntryDto.PriceListId, cancellationToken))
            {
                throw new ArgumentException($"Price list with ID {createPriceListEntryDto.PriceListId} does not exist.");
            }

            // Check if product exists
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
                Status = createPriceListEntryDto.Status,
                MinQuantity = createPriceListEntryDto.MinQuantity,
                MaxQuantity = createPriceListEntryDto.MaxQuantity,
                Notes = createPriceListEntryDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _context.PriceListEntries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created entry
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

            var entry = await _context.PriceListEntries
                .Where(ple => ple.Id == id && !ple.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Store original for audit
            var originalEntry = new PriceListEntry
            {
                Id = entry.Id,
                ProductId = entry.ProductId,
                PriceListId = entry.PriceListId,
                Price = entry.Price,
                Currency = entry.Currency,
                Score = entry.Score,
                IsEditableInFrontend = entry.IsEditableInFrontend,
                IsDiscountable = entry.IsDiscountable,
                Status = entry.Status,
                MinQuantity = entry.MinQuantity,
                MaxQuantity = entry.MaxQuantity,
                Notes = entry.Notes,
                CreatedBy = entry.CreatedBy,
                CreatedAt = entry.CreatedAt,
                ModifiedBy = entry.ModifiedBy,
                ModifiedAt = entry.ModifiedAt
            };

            // Update properties
            entry.Price = updatePriceListEntryDto.Price;
            entry.Currency = updatePriceListEntryDto.Currency;
            entry.Score = updatePriceListEntryDto.Score;
            entry.IsEditableInFrontend = updatePriceListEntryDto.IsEditableInFrontend;
            entry.IsDiscountable = updatePriceListEntryDto.IsDiscountable;
            entry.Status = updatePriceListEntryDto.Status;
            entry.MinQuantity = updatePriceListEntryDto.MinQuantity;
            entry.MaxQuantity = updatePriceListEntryDto.MaxQuantity;
            entry.Notes = updatePriceListEntryDto.Notes;
            entry.ModifiedBy = currentUser;
            entry.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated entry
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

            var entry = await _context.PriceListEntries
                .Where(ple => ple.Id == id && !ple.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry == null)
            {
                _logger.LogWarning("Price list entry with ID {EntryId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Store original for audit
            var originalEntry = new PriceListEntry
            {
                Id = entry.Id,
                ProductId = entry.ProductId,
                PriceListId = entry.PriceListId,
                Price = entry.Price,
                Currency = entry.Currency,
                Score = entry.Score,
                IsEditableInFrontend = entry.IsEditableInFrontend,
                IsDiscountable = entry.IsDiscountable,
                Status = entry.Status,
                MinQuantity = entry.MinQuantity,
                MaxQuantity = entry.MaxQuantity,
                Notes = entry.Notes,
                CreatedBy = entry.CreatedBy,
                CreatedAt = entry.CreatedAt,
                ModifiedBy = entry.ModifiedBy,
                ModifiedAt = entry.ModifiedAt,
                IsDeleted = entry.IsDeleted,
                DeletedBy = entry.DeletedBy,
                DeletedAt = entry.DeletedAt
            };

            // Soft delete the entry
            entry.IsDeleted = true;
            entry.DeletedBy = currentUser;
            entry.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted entry
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

    // Helper methods

    public async Task<bool> PriceListExistsAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceLists
            .AnyAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
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
            Status = priceList.Status,
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
            Status = priceList.Status,
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
            Status = entry.Status,
            MinQuantity = entry.MinQuantity,
            MaxQuantity = entry.MaxQuantity,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            CreatedBy = entry.CreatedBy,
            ModifiedAt = entry.ModifiedAt,
            ModifiedBy = entry.ModifiedBy
        };
    }
}