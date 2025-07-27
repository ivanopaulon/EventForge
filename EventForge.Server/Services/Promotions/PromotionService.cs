using EventForge.Server.DTOs.Promotions;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Promotions;

/// <summary>
/// Service implementation for managing promotions.
/// </summary>
public class PromotionService : IPromotionService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<PromotionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PromotionDto>> GetPromotionsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in promotion queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for promotion operations.");
            }

            var query = _context.Promotions
                .WhereActiveTenant(currentTenantId.Value)
                .Include(p => p.Rules.Where(pr => !pr.IsDeleted && pr.TenantId == currentTenantId.Value));

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

            if (promotion == null)
            {
                _logger.LogWarning("Promotion with ID {PromotionId} not found.", id);
                return null;
            }

            return MapToPromotionDto(promotion);
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

            if (originalPromotion == null)
            {
                _logger.LogWarning("Promotion with ID {PromotionId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var promotion = await _context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion == null)
            {
                _logger.LogWarning("Promotion with ID {PromotionId} not found for update by user {User}.", id, currentUser);
                return null;
            }

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

            if (originalPromotion == null)
            {
                _logger.LogWarning("Promotion with ID {PromotionId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var promotion = await _context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion == null)
            {
                _logger.LogWarning("Promotion with ID {PromotionId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

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

    public async Task<PromotionApplicationResultDto> ApplyPromotionRulesAsync(ApplyPromotionRulesDto applyDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement comprehensive promotion rule application logic
            // This is a basic implementation that can be expanded

            var result = new PromotionApplicationResultDto
            {
                OriginalTotal = applyDto.CartItems.Sum(item => item.UnitPrice * item.Quantity),
                Success = true
            };

            // Get applicable promotions
            var applicableRules = await GetApplicablePromotionRulesAsync(
                applyDto.CustomerId,
                applyDto.SalesChannel,
                applyDto.OrderDateTime,
                cancellationToken);

            // For now, just copy cart items without applying any rules
            result.CartItems = applyDto.CartItems.Select(item => new CartItemResultDto
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                CategoryIds = item.CategoryIds,
                ExistingLineDiscount = item.ExistingLineDiscount,
                OriginalLineTotal = item.UnitPrice * item.Quantity,
                FinalLineTotal = item.UnitPrice * item.Quantity * (1 - item.ExistingLineDiscount / 100m),
                PromotionDiscount = 0m,
                EffectiveDiscountPercentage = item.ExistingLineDiscount,
                AppliedPromotions = new List<AppliedPromotionDto>()
            }).ToList();

            result.FinalTotal = result.CartItems.Sum(item => item.FinalLineTotal);
            result.TotalDiscountAmount = result.OriginalTotal - result.FinalTotal;

            result.Messages.Add("Basic promotion application completed. Enhanced rule logic pending implementation.");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promotion rules.");
            return new PromotionApplicationResultDto
            {
                Success = false,
                Messages = { $"Error applying promotions: {ex.Message}" }
            };
        }
    }

    public async Task<IEnumerable<PromotionRuleDto>> GetApplicablePromotionRulesAsync(
        Guid? customerId = null,
        string? salesChannel = null,
        DateTime? orderDateTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var checkDateTime = orderDateTime ?? DateTime.UtcNow;

            var query = _context.PromotionRules
                .Include(pr => pr.Promotion)
                .Where(pr => !pr.IsDeleted &&
                           !pr.Promotion!.IsDeleted &&
                           pr.Promotion.StartDate <= checkDateTime &&
                           pr.Promotion.EndDate >= checkDateTime);

            // Apply filters based on parameters
            if (!string.IsNullOrEmpty(salesChannel) && salesChannel != "all")
            {
                query = query.Where(pr => pr.SalesChannels == null ||
                                        pr.SalesChannels.Contains(salesChannel));
            }

            var rules = await query.ToListAsync(cancellationToken);

            // TODO: Implement proper DTO mapping for PromotionRule
            // For now, return empty collection to avoid compilation errors
            return new List<PromotionRuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applicable promotion rules.");
            throw;
        }
    }
}