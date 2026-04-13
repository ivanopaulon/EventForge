using Prym.DTOs.Sales;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing note flags.
/// </summary>
public class NoteFlagService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<NoteFlagService> logger) : INoteFlagService
{

    public async Task<PagedResult<NoteFlagDto>> GetNoteFlagsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var query = context.NoteFlags
                .AsNoTracking()
                .Where(nf => nf.TenantId == currentTenantId.Value && !nf.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);

            var noteFlags = await query
                .OrderBy(nf => nf.DisplayOrder)
                .ThenBy(nf => nf.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<NoteFlagDto>
            {
                Items = noteFlags.Select(MapToDto),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<NoteFlagDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var noteFlags = await context.NoteFlags
                .AsNoTracking()
                .Where(nf => nf.TenantId == currentTenantId.Value && !nf.IsDeleted)
                .OrderBy(nf => nf.DisplayOrder)
                .ThenBy(nf => nf.Name)
                .ToListAsync(cancellationToken);

            return noteFlags.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<NoteFlagDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var noteFlags = await context.NoteFlags
                .AsNoTracking()
                .Where(nf => nf.TenantId == currentTenantId.Value && !nf.IsDeleted && nf.IsActive)
                .OrderBy(nf => nf.DisplayOrder)
                .ThenBy(nf => nf.Name)
                .ToListAsync(cancellationToken);

            return noteFlags.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<NoteFlagDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var noteFlag = await context.NoteFlags
                .AsNoTracking()
                .FirstOrDefaultAsync(nf => nf.Id == id && nf.TenantId == currentTenantId.Value && !nf.IsDeleted, cancellationToken);

            return noteFlag is null ? null : MapToDto(noteFlag);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<NoteFlagDto> CreateAsync(CreateNoteFlagDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            // Check if code already exists
            var codeExists = await context.NoteFlags
                .AnyAsync(nf => nf.Code == createDto.Code && nf.TenantId == currentTenantId.Value && !nf.IsDeleted, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"A note flag with code '{createDto.Code}' already exists.");
            }

            var noteFlag = new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                Icon = createDto.Icon,
                IsActive = createDto.IsActive,
                DisplayOrder = createDto.DisplayOrder,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _ = context.NoteFlags.Add(noteFlag);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("NoteFlag", noteFlag.Id, "Code", "Create", null, createDto.Code, currentUser, "Note Flag", cancellationToken);

            logger.LogInformation("Created note flag {NoteFlagId} with code {Code}", noteFlag.Id, createDto.Code);

            return MapToDto(noteFlag);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var noteFlag = await context.NoteFlags
                .FirstOrDefaultAsync(nf => nf.Id == id && nf.TenantId == currentTenantId.Value && !nf.IsDeleted, cancellationToken);

            if (noteFlag is null)
            {
                return null;
            }

            noteFlag.Name = updateDto.Name;
            noteFlag.Description = updateDto.Description;
            noteFlag.Color = updateDto.Color;
            noteFlag.Icon = updateDto.Icon;
            noteFlag.IsActive = updateDto.IsActive;
            noteFlag.DisplayOrder = updateDto.DisplayOrder;
            noteFlag.ModifiedBy = currentUser;
            noteFlag.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating NoteFlag {NoteFlagId}.", id);
                throw new InvalidOperationException("Il flag nota è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.LogEntityChangeAsync("NoteFlag", noteFlag.Id, "Name", "Update", null, updateDto.Name, currentUser, "Note Flag", cancellationToken);

            logger.LogInformation("Updated note flag {NoteFlagId}", id);

            return MapToDto(noteFlag);
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

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for note flag operations.");
            }

            var noteFlag = await context.NoteFlags
                .FirstOrDefaultAsync(nf => nf.Id == id && nf.TenantId == currentTenantId.Value && !nf.IsDeleted, cancellationToken);

            if (noteFlag is null)
            {
                return false;
            }

            noteFlag.IsDeleted = true;
            noteFlag.DeletedAt = DateTime.UtcNow;
            noteFlag.DeletedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting NoteFlag {NoteFlagId}.", id);
                throw new InvalidOperationException("Il flag nota è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.LogEntityChangeAsync("NoteFlag", noteFlag.Id, "IsDeleted", "Delete", "false", "true", currentUser, "Note Flag", cancellationToken);

            logger.LogInformation("Deleted note flag {NoteFlagId}", id);

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

    private NoteFlagDto MapToDto(NoteFlag noteFlag)
    {
        return new NoteFlagDto
        {
            Id = noteFlag.Id,
            Code = noteFlag.Code,
            Name = noteFlag.Name,
            Description = noteFlag.Description,
            Color = noteFlag.Color,
            Icon = noteFlag.Icon,
            IsActive = noteFlag.IsActive,
            DisplayOrder = noteFlag.DisplayOrder
        };
    }

}
