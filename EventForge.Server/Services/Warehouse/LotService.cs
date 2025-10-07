using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing lots and traceability.
/// </summary>
public class LotService : ILotService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LotService> _logger;

    public LotService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<LotService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<LotDto>> GetLotsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        string? status = null,
        bool? expiringSoon = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Lots
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .Where(l => l.TenantId == currentTenantId.Value && !l.IsDeleted);

            // Apply filters
            if (productId.HasValue)
            {
                query = query.Where(l => l.ProductId == productId.Value);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LotStatus>(status, out var lotStatus))
            {
                query = query.Where(l => l.Status == lotStatus);
            }

            if (expiringSoon.HasValue && expiringSoon.Value)
            {
                var expiryThreshold = DateTime.UtcNow.AddDays(30);
                query = query.Where(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value <= expiryThreshold);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering
            var lots = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var lotDtos = lots.Select(LotMapper.ToDto).ToList();

            return new PagedResult<LotDto>
            {
                Items = lotDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lots for tenant {TenantId}", _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            return lot != null ? LotMapper.ToDto(lot) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lot {LotId} for tenant {TenantId}", id, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .FirstOrDefaultAsync(l => l.Code == code && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            return lot != null ? LotMapper.ToDto(lot) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lot by code {Code} for tenant {TenantId}", code, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<IEnumerable<LotDto>> GetLotsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lots = await _context.Lots
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .Where(l => l.ProductId == productId && l.TenantId == currentTenantId.Value && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            return lots.Select(LotMapper.ToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lots for product {ProductId} and tenant {TenantId}", productId, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var expiryThreshold = DateTime.UtcNow.AddDays(daysAhead);

            var lots = await _context.Lots
                .Include(l => l.Product)
                .Include(l => l.Supplier)
                .Where(l => l.TenantId == currentTenantId.Value &&
                           !l.IsDeleted &&
                           l.Status == LotStatus.Active &&
                           l.ExpiryDate.HasValue &&
                           l.ExpiryDate.Value <= expiryThreshold)
                .OrderBy(l => l.ExpiryDate)
                .ToListAsync(cancellationToken);

            return lots.Select(LotMapper.ToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring lots for tenant {TenantId}", _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Check if lot code is unique
            var existingLot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Code == createDto.Code && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (existingLot != null)
            {
                throw new InvalidOperationException($"A lot with code '{createDto.Code}' already exists.");
            }

            // Verify product exists
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == createDto.ProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID '{createDto.ProductId}' not found.");
            }

            var lot = LotMapper.ToEntity(createDto, currentTenantId.Value, currentUser);

            _ = _context.Lots.Add(lot);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "Create",
                "Create",
                null,
                $"Created lot {lot.Code} for product {product.Name}",
                currentUser);

            _logger.LogInformation("Created lot {LotId} with code {Code} for tenant {TenantId}", lot.Id, lot.Code, currentTenantId.Value);

            // Return with loaded navigation properties
            var createdLot = await GetLotByIdAsync(lot.Id, cancellationToken);
            return createdLot!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot with code {Code} for tenant {TenantId}", createDto.Code, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (lot == null)
            {
                return null;
            }

            // Check if lot code is unique (excluding current lot)
            var existingLot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Code == updateDto.Code && l.Id != id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (existingLot != null)
            {
                throw new InvalidOperationException($"A lot with code '{updateDto.Code}' already exists.");
            }

            var originalCode = lot.Code;
            LotMapper.UpdateEntity(lot, updateDto, currentUser);

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "Update",
                "Update",
                originalCode,
                $"Updated lot from {originalCode} to {lot.Code}",
                currentUser);

            _logger.LogInformation("Updated lot {LotId} for tenant {TenantId}", lot.Id, currentTenantId.Value);

            return await GetLotByIdAsync(lot.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lot {LotId} for tenant {TenantId}", id, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (lot == null)
            {
                return false;
            }

            // Check if lot has stock movements
            var hasMovements = await _context.StockMovements
                .AnyAsync(sm => sm.LotId == id && sm.TenantId == currentTenantId.Value, cancellationToken);

            if (hasMovements)
            {
                throw new InvalidOperationException("Cannot delete lot that has stock movements. Set status to inactive instead.");
            }

            // Soft delete
            lot.IsDeleted = true;
            lot.DeletedAt = DateTime.UtcNow;
            lot.DeletedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "Delete",
                "Delete",
                lot.Code,
                "Deleted",
                currentUser);

            _logger.LogInformation("Deleted lot {LotId} for tenant {TenantId}", lot.Id, currentTenantId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lot {LotId} for tenant {TenantId}", id, _tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            if (!Enum.TryParse<QualityStatus>(qualityStatus, out var status))
            {
                throw new ArgumentException($"Invalid quality status: {qualityStatus}");
            }

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (lot == null)
            {
                return false;
            }

            var originalStatus = lot.QualityStatus;
            lot.QualityStatus = status;
            lot.ModifiedBy = currentUser;
            lot.ModifiedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(notes))
            {
                lot.Notes = notes;
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "QualityStatus",
                "Update",
                originalStatus.ToString(),
                status.ToString(),
                currentUser);

            _logger.LogInformation("Updated quality status for lot {LotId} from {OriginalStatus} to {NewStatus}",
                lot.Id, originalStatus, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quality status for lot {LotId}", id);
            throw;
        }
    }

    public async Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (lot == null)
            {
                return false;
            }

            lot.Status = LotStatus.Blocked;
            lot.Notes = $"{lot.Notes}\n[BLOCKED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {reason}".Trim();
            lot.ModifiedBy = currentUser;
            lot.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "Status",
                "Update",
                "Active",
                "Blocked",
                currentUser);

            _logger.LogWarning("Blocked lot {LotId} ({Code}). Reason: {Reason}", lot.Id, lot.Code, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking lot {LotId}", id);
            throw;
        }
    }

    public async Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var lot = await _context.Lots
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (lot == null)
            {
                return false;
            }

            lot.Status = LotStatus.Active;
            lot.Notes = $"{lot.Notes}\n[UNBLOCKED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: Lot unblocked by {currentUser}".Trim();
            lot.ModifiedBy = currentUser;
            lot.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit event
            _ = await _auditLogService.LogEntityChangeAsync(
                "Lot",
                lot.Id,
                "Status",
                "Update",
                "Blocked",
                "Active",
                currentUser);

            _logger.LogInformation("Unblocked lot {LotId} ({Code})", lot.Id, lot.Code);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking lot {LotId}", id);
            throw;
        }
    }

    public async Task<decimal> GetAvailableQuantityAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var totalQuantity = await _context.Stocks
                .Where(s => s.LotId == lotId && s.TenantId == currentTenantId.Value)
                .SumAsync(s => s.AvailableQuantity, cancellationToken);

            return totalQuantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available quantity for lot {LotId}", lotId);
            throw;
        }
    }

    public async Task<bool> IsLotCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Lots
                .Where(l => l.Code == code && l.TenantId == currentTenantId.Value && !l.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(l => l.Id != excludeId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);
            return !exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lot code uniqueness for {Code}", code);
            throw;
        }
    }
}