using Prym.DTOs.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.UnitOfMeasures;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService(
    PrymDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<UMService> logger) : IUMService
{

    public async Task<PagedResult<UMDto>> GetUMsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var query = context.UMs
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Where(u => !u.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var ums = await query
                .OrderBy(u => u.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var umDtos = ums.Select(MapToUMDto);

            return new PagedResult<UMDto>
            {
                Items = umDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving units of measure.");
            throw;
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var um = await context.UMs
                .Where(u => u.Id == id && u.TenantId == currentTenantId.Value && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return um is not null ? MapToUMDto(um) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createUMDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var um = new UM
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createUMDto.Name,
                Symbol = createUMDto.Symbol,
                Description = createUMDto.Description,
                IsDefault = createUMDto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.UMs.Add(um);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(um, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Unit of measure {UMId} created by {User}.", um.Id, currentUser);

            return MapToUMDto(um);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating unit of measure.");
            throw;
        }
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateUMDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalUM = await context.UMs
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalUM is null) return null;

            var um = await context.UMs
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (um is null) return null;

            um.Name = updateUMDto.Name;
            // Note: Symbol is intentionally not updatable - it's used in calculations
            um.Description = updateUMDto.Description;
            um.IsDefault = updateUMDto.IsDefault;
            um.ModifiedAt = DateTime.UtcNow;
            um.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating UM {UMId}.", id);
                throw new InvalidOperationException("L'unità di misura è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(um, "Update", currentUser, originalUM, cancellationToken);

            logger.LogInformation("Unit of measure {UMId} updated by {User}.", um.Id, currentUser);

            return MapToUMDto(um);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteUMAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalUM = await context.UMs
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalUM is null) return false;

            var um = await context.UMs
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (um is null) return false;

            um.IsDeleted = true;
            um.ModifiedAt = DateTime.UtcNow;
            um.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting UM {UMId}.", id);
                throw new InvalidOperationException("L'unità di misura è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(um, "Delete", currentUser, originalUM, cancellationToken);

            logger.LogInformation("Unit of measure {UMId} deleted by {User}.", um.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<bool> UMExistsAsync(Guid umId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.UMs
                .AnyAsync(u => u.Id == umId && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if unit of measure {UMId} exists.", umId);
            throw;
        }
    }

    private static UMDto MapToUMDto(UM um)
    {
        return new UMDto
        {
            Id = um.Id,
            Name = um.Name,
            Symbol = um.Symbol,
            Description = um.Description,
            IsDefault = um.IsDefault,
            IsActive = um.IsActive,
            CreatedAt = um.CreatedAt,
            CreatedBy = um.CreatedBy,
            ModifiedAt = um.ModifiedAt,
            ModifiedBy = um.ModifiedBy
        };
    }

}
