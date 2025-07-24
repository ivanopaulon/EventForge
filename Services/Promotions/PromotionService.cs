using EventForge.DTOs.Promotions;
using EventForge.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Promotions;

/// <summary>
/// Service implementation for managing promotions.
/// </summary>
public class PromotionService : IPromotionService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(EventForgeDbContext context, IAuditLogService auditLogService, ILogger<PromotionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PromotionDto>> GetPromotionsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Promotions
                .Where(p => !p.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var promotions = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var promotionDtos = promotions.Select(MapToPromotionDto);

            return new PagedResult<PromotionDto>
            {
                Items = promotionDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotions.");
            throw;
        }
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await _context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return promotion == null ? null : MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .Where(p => !p.IsDeleted && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return promotions.Select(MapToPromotionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active promotions.");
            throw;
        }
    }

    public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                MinOrderAmount = createDto.MinOrderAmount,
                MaxUses = createDto.MaxUses,
                CouponCode = createDto.CouponCode,
                Priority = createDto.Priority,
                IsCombinable = createDto.IsCombinable,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(promotion, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Promotion {PromotionId} created by {User}.", promotion.Id, currentUser);

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion.");
            throw;
        }
    }

    public async Task<PromotionDto?> UpdatePromotionAsync(Guid id, UpdatePromotionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPromotion = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPromotion == null) return null;

            var promotion = await _context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion == null) return null;

            promotion.Name = updateDto.Name;
            promotion.Description = updateDto.Description;
            promotion.StartDate = updateDto.StartDate;
            promotion.EndDate = updateDto.EndDate;
            promotion.MinOrderAmount = updateDto.MinOrderAmount;
            promotion.MaxUses = updateDto.MaxUses;
            promotion.CouponCode = updateDto.CouponCode;
            promotion.Priority = updateDto.Priority;
            promotion.IsCombinable = updateDto.IsCombinable;
            promotion.ModifiedAt = DateTime.UtcNow;
            promotion.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(promotion, "Update", currentUser, originalPromotion, cancellationToken);

            _logger.LogInformation("Promotion {PromotionId} updated by {User}.", promotion.Id, currentUser);

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<bool> DeletePromotionAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPromotion = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPromotion == null) return false;

            var promotion = await _context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion == null) return false;

            promotion.IsDeleted = true;
            promotion.DeletedAt = DateTime.UtcNow;
            promotion.DeletedBy = currentUser;
            promotion.ModifiedAt = DateTime.UtcNow;
            promotion.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(promotion, "Delete", currentUser, originalPromotion, cancellationToken);

            _logger.LogInformation("Promotion {PromotionId} deleted by {User}.", promotion.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<bool> PromotionExistsAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Promotions
                .AnyAsync(p => p.Id == promotionId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if promotion {PromotionId} exists.", promotionId);
            throw;
        }
    }

    private static PromotionDto MapToPromotionDto(Promotion promotion)
    {
        return new PromotionDto
        {
            Id = promotion.Id,
            Name = promotion.Name,
            Description = promotion.Description,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            MinOrderAmount = promotion.MinOrderAmount,
            MaxUses = promotion.MaxUses,
            CouponCode = promotion.CouponCode,
            Priority = promotion.Priority,
            IsCombinable = promotion.IsCombinable,
            CreatedAt = promotion.CreatedAt,
            CreatedBy = promotion.CreatedBy,
            ModifiedAt = promotion.ModifiedAt,
            ModifiedBy = promotion.ModifiedBy
        };
    }
}