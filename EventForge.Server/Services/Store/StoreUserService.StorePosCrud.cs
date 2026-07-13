using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{

    public async Task<PagedResult<StorePosDto>> GetStorePosesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        logger.LogDebug("Querying store POS terminals for tenant {TenantId}", currentTenantId.Value);

        var query = context.StorePoses
            .AsNoTracking()
            .Where(sp => !sp.IsDeleted && sp.TenantId == currentTenantId.Value)
            .OrderBy(sp => sp.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        logger.LogDebug("Found {Count} store POS terminals for tenant {TenantId}", totalCount, currentTenantId.Value);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToStorePosDto).ToList();

        return new PagedResult<StorePosDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StorePosDto?> GetStorePosByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .AsNoTracking()
            .Include(sp => sp.ImageDocument)
            .Include(sp => sp.DefaultFiscalPrinter)
            .Include(sp => sp.DefaultPaymentTerminal)
            .Include(sp => sp.CashierGroup)
            .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        return storePos is not null ? MapToStorePosDto(storePos) : null;
    }

    public async Task<StorePosDto> CreateStorePosAsync(CreateStorePosDto createStorePosDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = new StorePos
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId.Value,
            Name = createStorePosDto.Name,
            Description = createStorePosDto.Description,
            Status = (EventForge.Server.Data.Entities.Store.CashRegisterStatus)createStorePosDto.Status,
            Location = createStorePosDto.Location,
            Notes = createStorePosDto.Notes,
            TerminalIdentifier = createStorePosDto.TerminalIdentifier,
            IPAddress = createStorePosDto.IPAddress,
            LocationLatitude = createStorePosDto.LocationLatitude,
            LocationLongitude = createStorePosDto.LocationLongitude,
            CurrencyCode = createStorePosDto.CurrencyCode,
            TimeZone = createStorePosDto.TimeZone,
            ImageDocumentId = createStorePosDto.ImageDocumentId,
            DefaultFiscalPrinterId = createStorePosDto.DefaultFiscalPrinterId,
            CashierGroupId = createStorePosDto.CashierGroupId,
            DefaultPaymentTerminalId = createStorePosDto.DefaultPaymentTerminalId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            IsDeleted = false
        };

        context.StorePoses.Add(storePos);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store POS {Name} created successfully by {User}.", storePos.Name, currentUser);
        return MapToStorePosDto(storePos);
    }

    public async Task<StorePosDto?> UpdateStorePosAsync(Guid id, UpdateStorePosDto updateStorePosDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        if (storePos is null)
        {
            logger.LogWarning("Store POS {Id} not found for update in tenant {TenantId}.", id, currentTenantId.Value);
            return null;
        }

        storePos.Name = updateStorePosDto.Name;
        storePos.Description = updateStorePosDto.Description;
        storePos.Status = (EventForge.Server.Data.Entities.Store.CashRegisterStatus)updateStorePosDto.Status;
        storePos.Location = updateStorePosDto.Location;
        storePos.Notes = updateStorePosDto.Notes;
        storePos.TerminalIdentifier = updateStorePosDto.TerminalIdentifier;
        storePos.IPAddress = updateStorePosDto.IPAddress;
        storePos.IsOnline = updateStorePosDto.IsOnline;
        storePos.ImageDocumentId = updateStorePosDto.ImageDocumentId;
        storePos.DefaultFiscalPrinterId = updateStorePosDto.DefaultFiscalPrinterId;
        storePos.CashierGroupId = updateStorePosDto.CashierGroupId;
        storePos.DefaultPaymentTerminalId = updateStorePosDto.DefaultPaymentTerminalId;
        storePos.ModifiedAt = DateTime.UtcNow;
        storePos.ModifiedBy = currentUser;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store POS {Id} updated successfully by {User}.", id, currentUser);
        return MapToStorePosDto(storePos);
    }

    public async Task<bool> DeleteStorePosAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        if (storePos is null)
        {
            logger.LogWarning("Store POS {Id} not found for deletion in tenant {TenantId}.", id, currentTenantId.Value);
            return false;
        }

        storePos.IsDeleted = true;
        storePos.ModifiedAt = DateTime.UtcNow;
        storePos.ModifiedBy = currentUser;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store POS {Id} deleted successfully by {User}.", id, currentUser);
        return true;
    }

}
