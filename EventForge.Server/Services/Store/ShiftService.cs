using Prym.DTOs.Store;
using EventForge.Server.Data.Entities.Store;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service implementation for managing cashier shift scheduling.
/// </summary>
public class ShiftService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<ShiftService> logger) : IShiftService
{
    public async Task<List<CashierShiftDto>> GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var entities = await context.CashierShifts
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Include(s => s.StoreUser)
            .Include(s => s.Pos)
            .Where(s => s.ShiftStart <= toUtc && s.ShiftEnd >= fromUtc)
            .OrderBy(s => s.ShiftStart)
            .ToListAsync(ct);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<CashierShiftDto?> GetShiftByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var entity = await context.CashierShifts
            .AsNoTracking()
            .Include(s => s.StoreUser)
            .Include(s => s.Pos)
            .Where(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            logger.LogWarning("CashierShift {ShiftId} not found for tenant {TenantId}.", id, tenantId);
            return null;
        }

        return MapToDto(entity);
    }

    public async Task<CashierShiftDto> CreateShiftAsync(CreateCashierShiftDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        if (dto.ShiftEnd <= dto.ShiftStart)
            throw new ArgumentException("Shift end must be after shift start.");

        // Verify the store user belongs to this tenant
        var userExists = await context.StoreUsers
            .AnyAsync(u => u.Id == dto.StoreUserId && u.TenantId == tenantId && !u.IsDeleted, ct);
        if (!userExists)
            throw new InvalidOperationException($"Store user {dto.StoreUserId} not found.");

        var entity = new CashierShift
        {
            TenantId = tenantId,
            StoreUserId = dto.StoreUserId,
            PosId = dto.PosId,
            ShiftStart = dto.ShiftStart,
            ShiftEnd = dto.ShiftEnd,
            Status = ShiftStatus.Scheduled,
            Notes = dto.Notes,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        context.CashierShifts.Add(entity);
        await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, ct);

        // Reload with navigation properties
        await context.Entry(entity).Reference(s => s.StoreUser).LoadAsync(ct);
        if (entity.PosId.HasValue)
            await context.Entry(entity).Reference(s => s.Pos).LoadAsync(ct);

        logger.LogInformation("CashierShift {ShiftId} created by {User}.", entity.Id, currentUser);
        return MapToDto(entity);
    }

    public async Task<CashierShiftDto?> UpdateShiftAsync(Guid id, UpdateCashierShiftDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        if (dto.ShiftEnd <= dto.ShiftStart)
            throw new ArgumentException("Shift end must be after shift start.");

        var entity = await context.CashierShifts
            .Include(s => s.StoreUser)
            .Include(s => s.Pos)
            .Where(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            logger.LogWarning("CashierShift {ShiftId} not found for update.", id);
            return null;
        }

        entity.PosId = dto.PosId;
        entity.ShiftStart = dto.ShiftStart;
        entity.ShiftEnd = dto.ShiftEnd;
        entity.Status = dto.Status;
        entity.Notes = dto.Notes;
        entity.ModifiedBy = currentUser;
        entity.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, null, ct);

        // Reload navigation properties after PosId change
        await context.Entry(entity).Reference(s => s.StoreUser).LoadAsync(ct);
        if (entity.PosId.HasValue)
            await context.Entry(entity).Reference(s => s.Pos).LoadAsync(ct);
        else
            entity.Pos = null;

        logger.LogInformation("CashierShift {ShiftId} updated by {User}.", id, currentUser);
        return MapToDto(entity);
    }

    public async Task<bool> DeleteShiftAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var entity = await context.CashierShifts
            .Where(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            logger.LogWarning("CashierShift {ShiftId} not found for deletion.", id);
            return false;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = currentUser;
        entity.IsActive = false;

        await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, null, ct);

        logger.LogInformation("CashierShift {ShiftId} soft-deleted by {User}.", id, currentUser);
        return true;
    }

    public async Task<List<CashierShiftDto>> GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var entities = await context.CashierShifts
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Include(s => s.StoreUser)
            .Include(s => s.Pos)
            .Where(s => s.StoreUserId == storeUserId && s.ShiftStart <= toUtc && s.ShiftEnd >= fromUtc)
            .OrderBy(s => s.ShiftStart)
            .ToListAsync(ct);

        return entities.Select(MapToDto).ToList();
    }

    #region Helpers

    private Guid RequireTenantId()
    {
        if (!tenantContext.CurrentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for shift operations.");
        return tenantContext.CurrentTenantId.Value;
    }

    private static CashierShiftDto MapToDto(CashierShift s) => new()
    {
        Id = s.Id,
        StoreUserId = s.StoreUserId,
        StoreUserName = s.StoreUser?.Name ?? string.Empty,
        PosId = s.PosId,
        PosName = s.Pos?.Name,
        ShiftStart = s.ShiftStart,
        ShiftEnd = s.ShiftEnd,
        Status = s.Status,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt
    };

    #endregion
}
