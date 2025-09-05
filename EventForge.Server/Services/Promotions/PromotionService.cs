using EventForge.DTOs.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
    private readonly IMemoryCache _cache;

    public PromotionService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<PromotionService> logger, IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

            // Invalidate cache after creating promotion
            InvalidatePromotionCache();

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

            // Invalidate cache after updating promotion
            InvalidatePromotionCache();

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

            // Invalidate cache after deleting promotion
            InvalidatePromotionCache();

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
            _logger.LogDebug("Starting promotion rule application for {ItemCount} cart items", applyDto.CartItems.Count);

            // Step 1: Validate input
            var validationResult = ValidateApplyPromotionInput(applyDto);
            if (!validationResult.IsValid)
            {
                return new PromotionApplicationResultDto
                {
                    Success = false,
                    Messages = validationResult.Errors
                };
            }

            // Step 2: Initialize result with original totals
            var result = new PromotionApplicationResultDto
            {
                OriginalTotal = applyDto.CartItems.Sum(item => item.UnitPrice * item.Quantity),
                Success = true
            };

            // Step 3: Get applicable promotions with caching
            var applicablePromotions = await GetCachedActivePromotionsAsync(cancellationToken);
            var (filteredPromotions, exclusionMessages) = FilterApplicablePromotionsWithMessages(applicablePromotions, applyDto);

            // Add exclusion messages to result
            result.Messages.AddRange(exclusionMessages);

            // Step 4: Order by priority (desc) then by name
            var orderedPromotions = filteredPromotions
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            _logger.LogDebug("Found {PromotionCount} applicable promotions", orderedPromotions.Count);

            // Step 5: Initialize cart items with existing discounts
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
                FinalLineTotal = RoundCurrency(item.UnitPrice * item.Quantity * (1 - item.ExistingLineDiscount / 100m)),
                PromotionDiscount = 0m,
                EffectiveDiscountPercentage = item.ExistingLineDiscount,
                AppliedPromotions = new List<AppliedPromotionDto>()
            }).ToList();

            // Step 6: Apply promotions with precedence and combinability logic
            var lockedLines = new HashSet<Guid>(); // Track lines affected by non-combinable promotions
            bool exclusiveApplied = false;

            foreach (var promotion in orderedPromotions)
            {
                if (exclusiveApplied)
                {
                    result.Messages.Add($"Skipped promotion '{promotion.Name}' due to exclusive promotion already applied");
                    continue;
                }

                var applied = ApplyPromotionToCart(promotion, result.CartItems, applyDto, lockedLines, result);

                if (applied && promotion.Rules.Any(r => r.RuleType == EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive))
                {
                    exclusiveApplied = true;
                    result.Messages.Add($"Exclusive promotion '{promotion.Name}' applied - stopping further applications");
                }
            }

            // Step 7: Calculate final totals
            result.FinalTotal = result.CartItems.Sum(item => item.FinalLineTotal);
            result.TotalDiscountAmount = result.OriginalTotal - result.FinalTotal;

            _logger.LogInformation("Promotion application completed. Original: {Original}, Final: {Final}, Discount: {Discount}",
                result.OriginalTotal, result.FinalTotal, result.TotalDiscountAmount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying promotion rules");
            return new PromotionApplicationResultDto
            {
                Success = false,
                Messages = { $"Error applying promotions: {ex.Message}" }
            };
        }
    }

    #region Promotion Application Helper Methods

    /// <summary>
    /// Validates the input for promotion rule application.
    /// </summary>
    private (bool IsValid, List<string> Errors) ValidateApplyPromotionInput(ApplyPromotionRulesDto applyDto)
    {
        var errors = new List<string>();

        // Validate currency
        if (string.IsNullOrWhiteSpace(applyDto.Currency))
        {
            errors.Add("Currency is required");
        }

        // Validate cart items
        if (applyDto.CartItems == null || !applyDto.CartItems.Any())
        {
            errors.Add("Cart cannot be empty");
        }
        else
        {
            foreach (var item in applyDto.CartItems)
            {
                if (item.UnitPrice < 0)
                {
                    errors.Add($"Unit price cannot be negative for product {item.ProductName}");
                }
                if (item.Quantity <= 0)
                {
                    errors.Add($"Quantity must be positive for product {item.ProductName}");
                }
                if (item.ExistingLineDiscount < 0 || item.ExistingLineDiscount > 100)
                {
                    errors.Add($"Existing discount percentage must be between 0 and 100 for product {item.ProductName}");
                }
            }
        }

        // Validate coupon codes format if provided
        if (applyDto.CouponCodes != null)
        {
            foreach (var coupon in applyDto.CouponCodes)
            {
                if (string.IsNullOrWhiteSpace(coupon) || coupon.Length > 50)
                {
                    errors.Add($"Invalid coupon code format: {coupon}");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Gets cached active promotions for the current tenant.
    /// </summary>
    private async Task<List<Promotion>> GetCachedActivePromotionsAsync(CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for promotion operations");
        }

        var cacheKey = $"ActivePromotions:{currentTenantId.Value}";

        if (_cache.TryGetValue(cacheKey, out List<Promotion>? cachedPromotions))
        {
            _logger.LogDebug("Retrieved {Count} promotions from cache", cachedPromotions?.Count ?? 0);
            return cachedPromotions ?? new List<Promotion>();
        }

        _logger.LogDebug("Cache miss - fetching promotions from database");

        var now = DateTime.UtcNow;
        var promotions = await _context.Promotions
            .WhereActiveTenant(currentTenantId.Value)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .Include(p => p.Rules.Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value))
                .ThenInclude(r => r.Products)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60), // 60 second TTL
            SlidingExpiration = TimeSpan.FromSeconds(30),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, promotions, cacheOptions);
        _logger.LogDebug("Cached {Count} promotions", promotions.Count);

        return promotions;
    }

    /// <summary>
    /// Filters promotions based on the apply context and returns exclusion messages.
    /// </summary>
    private (List<Promotion>, List<string>) FilterApplicablePromotionsWithMessages(List<Promotion> promotions, ApplyPromotionRulesDto applyDto)
    {
        var filtered = new List<Promotion>();
        var exclusionMessages = new List<string>();
        var appliedCoupons = applyDto.CouponCodes?.Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        foreach (var promotion in promotions)
        {
            // Check if promotion requires coupon
            if (!string.IsNullOrEmpty(promotion.CouponCode))
            {
                if (!appliedCoupons.Contains(promotion.CouponCode))
                {
                    _logger.LogDebug("Skipping promotion {PromotionName} - required coupon {Coupon} not provided",
                        promotion.Name, promotion.CouponCode);
                    continue;
                }
            }

            // Check minimum order amount
            if (promotion.MinOrderAmount.HasValue)
            {
                var currentTotal = applyDto.CartItems.Sum(item => item.UnitPrice * item.Quantity);
                if (currentTotal < promotion.MinOrderAmount.Value)
                {
                    _logger.LogDebug("Skipping promotion {PromotionName} - minimum order amount {MinAmount} not met (current: {CurrentTotal})",
                        promotion.Name, promotion.MinOrderAmount.Value, currentTotal);
                    exclusionMessages.Add($"Cart total ${currentTotal:F2} doesn't meet minimum ${promotion.MinOrderAmount.Value:F2} for {promotion.Name}");
                    continue;
                }
            }

            // Filter rules based on context
            var applicableRules = promotion.Rules.Where(rule => IsRuleApplicable(rule, applyDto)).ToList();
            if (applicableRules.Any())
            {
                // Create a copy with only applicable rules
                var filteredPromotion = new Promotion
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
                    Rules = applicableRules
                };
                filtered.Add(filteredPromotion);
            }
        }

        return (filtered, exclusionMessages);
    }

    /// <summary>
    /// Filters promotions based on the apply context.
    /// </summary>
    private List<Promotion> FilterApplicablePromotions(List<Promotion> promotions, ApplyPromotionRulesDto applyDto)
    {
        var (filtered, _) = FilterApplicablePromotionsWithMessages(promotions, applyDto);
        return filtered;
    }

    /// <summary>
    /// Checks if a promotion rule is applicable to the current context.
    /// </summary>
    private bool IsRuleApplicable(PromotionRule rule, ApplyPromotionRulesDto applyDto)
    {
        // Check sales channel
        if (rule.SalesChannels != null && rule.SalesChannels.Any() && !string.IsNullOrEmpty(applyDto.SalesChannel))
        {
            if (!rule.SalesChannels.Contains(applyDto.SalesChannel, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check time restrictions
        if (rule.ValidDays != null && rule.ValidDays.Any())
        {
            var orderDayOfWeek = applyDto.OrderDateTime.DayOfWeek;
            if (!rule.ValidDays.Contains(orderDayOfWeek))
            {
                return false;
            }
        }

        if (rule.StartTime.HasValue && rule.EndTime.HasValue)
        {
            var orderTime = applyDto.OrderDateTime.TimeOfDay;
            if (orderTime < rule.StartTime.Value || orderTime > rule.EndTime.Value)
            {
                return false;
            }
        }

        // Check minimum order amount for rule
        if (rule.MinOrderAmount.HasValue)
        {
            var currentTotal = applyDto.CartItems.Sum(item => item.UnitPrice * item.Quantity);
            if (currentTotal < rule.MinOrderAmount.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Rounds currency values using midpoint rounding away from zero.
    /// </summary>
    private static decimal RoundCurrency(decimal value, int decimalPlaces = 2)
    {
        return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts entity PromotionRuleType to DTO PromotionRuleType.
    /// </summary>
    private static EventForge.DTOs.Common.PromotionRuleType ConvertRuleType(EventForge.Server.Data.Entities.Promotions.PromotionRuleType entityRuleType)
    {
        return entityRuleType switch
        {
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Discount => EventForge.DTOs.Common.PromotionRuleType.Discount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CategoryDiscount => EventForge.DTOs.Common.PromotionRuleType.CategoryDiscount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CartAmountDiscount => EventForge.DTOs.Common.PromotionRuleType.CartAmountDiscount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.BuyXGetY => EventForge.DTOs.Common.PromotionRuleType.BuyXGetY,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.FixedPrice => EventForge.DTOs.Common.PromotionRuleType.FixedPrice,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Bundle => EventForge.DTOs.Common.PromotionRuleType.Bundle,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CustomerSpecific => EventForge.DTOs.Common.PromotionRuleType.CustomerSpecific,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Coupon => EventForge.DTOs.Common.PromotionRuleType.Coupon,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.TimeLimited => EventForge.DTOs.Common.PromotionRuleType.TimeLimited,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive => EventForge.DTOs.Common.PromotionRuleType.Exclusive,
            _ => EventForge.DTOs.Common.PromotionRuleType.Discount
        };
    }

    /// <summary>
    /// Invalidates the promotion cache for the current tenant.
    /// </summary>
    private void InvalidatePromotionCache()
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (currentTenantId.HasValue)
        {
            var cacheKey = $"ActivePromotions:{currentTenantId.Value}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Invalidated promotion cache for tenant {TenantId}", currentTenantId.Value);
        }
    }

    #endregion

    #region Rule Application Methods

    /// <summary>
    /// Applies a promotion to the cart items.
    /// </summary>
    private bool ApplyPromotionToCart(Promotion promotion, List<CartItemResultDto> cartItems, ApplyPromotionRulesDto applyDto, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        bool anyApplied = false;

        foreach (var rule in promotion.Rules)
        {
            bool applied = rule.RuleType switch
            {
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Discount => ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CategoryDiscount => ApplyCategoryDiscountRule(rule, cartItems, promotion, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CartAmountDiscount => ApplyCartAmountDiscountRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.BuyXGetY => ApplyBuyXGetYRule(rule, cartItems, promotion, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.FixedPrice => ApplyFixedPriceRule(rule, cartItems, promotion, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Bundle => ApplyBundleRule(rule, cartItems, promotion, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Coupon => ApplyCouponRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.TimeLimited => ApplyTimeLimitedRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive => ApplyExclusiveRule(rule, cartItems, promotion, lockedLines, result),
                _ => false
            };

            if (applied)
            {
                anyApplied = true;

                // If promotion is not combinable, lock affected lines
                if (!promotion.IsCombinable)
                {
                    foreach (var item in cartItems.Where(c => c.AppliedPromotions.Any(ap => ap.PromotionId == promotion.Id)))
                    {
                        lockedLines.Add(item.ProductId);
                    }
                }
            }
        }

        return anyApplied;
    }

    private bool ApplyDiscountRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        bool applied = false;
        var targetItems = GetRuleTargetItems(rule, cartItems, lockedLines);

        foreach (var item in targetItems)
        {
            decimal discountAmount = 0;
            string description = "";

            if (rule.DiscountPercentage.HasValue)
            {
                discountAmount = RoundCurrency(item.FinalLineTotal * rule.DiscountPercentage.Value / 100m);
                description = $"{rule.DiscountPercentage.Value}% discount on {item.ProductName}";
            }
            else if (rule.DiscountAmount.HasValue)
            {
                discountAmount = Math.Min(rule.DiscountAmount.Value * item.Quantity, item.FinalLineTotal);
                description = $"${rule.DiscountAmount.Value} fixed discount on {item.ProductName}";
            }

            if (discountAmount > 0)
            {
                item.FinalLineTotal = RoundCurrency(Math.Max(0, item.FinalLineTotal - discountAmount));
                item.PromotionDiscount += discountAmount;
                item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

                var appliedPromotion = new AppliedPromotionDto
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    PromotionRuleId = rule.Id,
                    RuleType = ConvertRuleType(rule.RuleType),
                    DiscountAmount = discountAmount,
                    DiscountPercentage = rule.DiscountPercentage,
                    Description = description,
                    AffectedProductIds = { item.ProductId }
                };

                item.AppliedPromotions.Add(appliedPromotion);
                result.AppliedPromotions.Add(appliedPromotion);
                applied = true;

                _logger.LogDebug("Applied discount rule: {Description}, Amount: {Amount}", description, discountAmount);
            }
        }

        return applied;
    }

    private bool ApplyCategoryDiscountRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        if (rule.CategoryIds == null || !rule.CategoryIds.Any())
            return false;

        bool applied = false;
        var targetItems = cartItems.Where(item =>
            !lockedLines.Contains(item.ProductId) &&
            item.CategoryIds != null &&
            item.CategoryIds.Any(catId => rule.CategoryIds.Contains(catId))).ToList();

        foreach (var item in targetItems)
        {
            decimal discountAmount = 0;
            string description = "";

            if (rule.DiscountPercentage.HasValue)
            {
                discountAmount = RoundCurrency(item.FinalLineTotal * rule.DiscountPercentage.Value / 100m);
                description = $"{rule.DiscountPercentage.Value}% category discount on {item.ProductName}";
            }
            else if (rule.DiscountAmount.HasValue)
            {
                discountAmount = Math.Min(rule.DiscountAmount.Value * item.Quantity, item.FinalLineTotal);
                description = $"${rule.DiscountAmount.Value} category discount on {item.ProductName}";
            }

            if (discountAmount > 0)
            {
                item.FinalLineTotal = RoundCurrency(Math.Max(0, item.FinalLineTotal - discountAmount));
                item.PromotionDiscount += discountAmount;
                item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

                var appliedPromotion = new AppliedPromotionDto
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    PromotionRuleId = rule.Id,
                    RuleType = ConvertRuleType(rule.RuleType),
                    DiscountAmount = discountAmount,
                    DiscountPercentage = rule.DiscountPercentage,
                    Description = description,
                    AffectedProductIds = { item.ProductId }
                };

                item.AppliedPromotions.Add(appliedPromotion);
                result.AppliedPromotions.Add(appliedPromotion);
                applied = true;
            }
        }

        return applied;
    }

    private bool ApplyCartAmountDiscountRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, ApplyPromotionRulesDto applyDto, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        var currentTotal = cartItems.Sum(item => item.FinalLineTotal);

        // Check minimum order amount at rule level (if specified)
        if (rule.MinOrderAmount.HasValue && currentTotal < rule.MinOrderAmount.Value)
        {
            result.Messages.Add($"Cart total ${currentTotal:F2} doesn't meet minimum ${rule.MinOrderAmount.Value:F2} for {promotion.Name}");
            return false;
        }

        decimal totalDiscount = 0;
        string description = "";
        var minAmountForDescription = rule.MinOrderAmount ?? promotion.MinOrderAmount ?? 0m;

        if (rule.DiscountPercentage.HasValue)
        {
            totalDiscount = RoundCurrency(currentTotal * rule.DiscountPercentage.Value / 100m);
            description = $"{rule.DiscountPercentage.Value}% cart discount" + (minAmountForDescription > 0 ? $" (min ${minAmountForDescription:F2})" : "");
        }
        else if (rule.DiscountAmount.HasValue)
        {
            totalDiscount = Math.Min(rule.DiscountAmount.Value, currentTotal);
            description = $"${rule.DiscountAmount.Value:F2} cart discount" + (minAmountForDescription > 0 ? $" (min ${minAmountForDescription:F2})" : "");
        }

        if (totalDiscount > 0)
        {
            // Distribute discount proportionally across cart items
            var eligibleItems = cartItems.Where(item => !lockedLines.Contains(item.ProductId)).ToList();
            var totalEligibleAmount = eligibleItems.Sum(item => item.FinalLineTotal);

            foreach (var item in eligibleItems)
            {
                var proportion = item.FinalLineTotal / totalEligibleAmount;
                var itemDiscount = RoundCurrency(totalDiscount * proportion);

                item.FinalLineTotal = RoundCurrency(Math.Max(0, item.FinalLineTotal - itemDiscount));
                item.PromotionDiscount += itemDiscount;
                item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

                var appliedPromotion = new AppliedPromotionDto
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    PromotionRuleId = rule.Id,
                    RuleType = ConvertRuleType(rule.RuleType),
                    DiscountAmount = itemDiscount,
                    DiscountPercentage = rule.DiscountPercentage,
                    Description = description,
                    AffectedProductIds = { item.ProductId }
                };

                item.AppliedPromotions.Add(appliedPromotion);
            }

            result.AppliedPromotions.Add(new AppliedPromotionDto
            {
                PromotionId = promotion.Id,
                PromotionName = promotion.Name,
                PromotionRuleId = rule.Id,
                RuleType = ConvertRuleType(rule.RuleType),
                DiscountAmount = totalDiscount,
                DiscountPercentage = rule.DiscountPercentage,
                Description = description,
                AffectedProductIds = eligibleItems.Select(i => i.ProductId).ToList()
            });

            return true;
        }

        return false;
    }

    private bool ApplyBuyXGetYRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        if (!rule.RequiredQuantity.HasValue || !rule.FreeQuantity.HasValue)
            return false;

        bool applied = false;
        var targetItems = GetRuleTargetItems(rule, cartItems, lockedLines);

        foreach (var item in targetItems)
        {
            var eligibleSets = item.Quantity / rule.RequiredQuantity.Value;
            if (eligibleSets > 0)
            {
                var freeItems = eligibleSets * rule.FreeQuantity.Value;
                var discountAmount = RoundCurrency(item.UnitPrice * freeItems);

                item.FinalLineTotal = RoundCurrency(Math.Max(0, item.FinalLineTotal - discountAmount));
                item.PromotionDiscount += discountAmount;
                item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

                var description = $"Buy {rule.RequiredQuantity.Value} get {rule.FreeQuantity.Value} free on {item.ProductName}";

                var appliedPromotion = new AppliedPromotionDto
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    PromotionRuleId = rule.Id,
                    RuleType = ConvertRuleType(rule.RuleType),
                    DiscountAmount = discountAmount,
                    Description = description,
                    AffectedProductIds = { item.ProductId }
                };

                item.AppliedPromotions.Add(appliedPromotion);
                result.AppliedPromotions.Add(appliedPromotion);
                applied = true;

                _logger.LogDebug("Applied BuyXGetY rule: {Description}, Amount: {Amount}", description, discountAmount);
            }
        }

        return applied;
    }

    private bool ApplyFixedPriceRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        if (!rule.FixedPrice.HasValue)
            return false;

        bool applied = false;
        var targetItems = GetRuleTargetItems(rule, cartItems, lockedLines);

        foreach (var item in targetItems)
        {
            var newLineTotal = RoundCurrency(rule.FixedPrice.Value * item.Quantity);
            if (newLineTotal < item.FinalLineTotal)
            {
                var discountAmount = item.FinalLineTotal - newLineTotal;

                item.FinalLineTotal = newLineTotal;
                item.PromotionDiscount += discountAmount;
                item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

                var description = $"Fixed price ${rule.FixedPrice.Value:F2} per unit for {item.ProductName}";

                var appliedPromotion = new AppliedPromotionDto
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    PromotionRuleId = rule.Id,
                    RuleType = ConvertRuleType(rule.RuleType),
                    DiscountAmount = discountAmount,
                    Description = description,
                    AffectedProductIds = { item.ProductId }
                };

                item.AppliedPromotions.Add(appliedPromotion);
                result.AppliedPromotions.Add(appliedPromotion);
                applied = true;
            }
        }

        return applied;
    }

    private bool ApplyBundleRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        if (!rule.FixedPrice.HasValue || rule.Products == null || rule.Products.Count < 2)
            return false;

        // Find items that match the bundle requirements
        var bundleProductIds = rule.Products.Select(p => p.ProductId).ToHashSet();
        var bundleItems = cartItems.Where(item =>
            bundleProductIds.Contains(item.ProductId) &&
            !lockedLines.Contains(item.ProductId)).ToList();

        // Check if we have all required products in sufficient quantities
        var requiredProducts = rule.Products.ToList();
        bool canApplyBundle = true;

        foreach (var requiredProduct in requiredProducts)
        {
            var matchingItem = bundleItems.FirstOrDefault(i => i.ProductId == requiredProduct.ProductId);
            if (matchingItem == null || matchingItem.Quantity < (requiredProduct.Quantity ?? 1))
            {
                canApplyBundle = false;
                break;
            }
        }

        if (!canApplyBundle)
            return false;

        // Calculate bundle discount
        var bundleOriginalTotal = requiredProducts.Sum(rp =>
        {
            var item = bundleItems.First(i => i.ProductId == rp.ProductId);
            return item.UnitPrice * (rp.Quantity ?? 1);
        });

        var bundleDiscountAmount = RoundCurrency(bundleOriginalTotal - rule.FixedPrice.Value);
        if (bundleDiscountAmount <= 0)
            return false;

        // Apply discount proportionally to bundle items
        foreach (var requiredProduct in requiredProducts)
        {
            var item = bundleItems.First(i => i.ProductId == requiredProduct.ProductId);
            var requiredQty = requiredProduct.Quantity ?? 1;
            var itemOriginalTotal = item.UnitPrice * requiredQty;
            var proportion = itemOriginalTotal / bundleOriginalTotal;
            var itemDiscount = RoundCurrency(bundleDiscountAmount * proportion);

            item.FinalLineTotal = RoundCurrency(Math.Max(0, item.FinalLineTotal - itemDiscount));
            item.PromotionDiscount += itemDiscount;
            item.EffectiveDiscountPercentage = RoundCurrency((item.OriginalLineTotal - item.FinalLineTotal) / item.OriginalLineTotal * 100m);

            var description = $"Bundle discount: {promotion.Name}";

            var appliedPromotion = new AppliedPromotionDto
            {
                PromotionId = promotion.Id,
                PromotionName = promotion.Name,
                PromotionRuleId = rule.Id,
                RuleType = ConvertRuleType(rule.RuleType),
                DiscountAmount = itemDiscount,
                Description = description,
                AffectedProductIds = { item.ProductId }
            };

            item.AppliedPromotions.Add(appliedPromotion);
        }

        result.AppliedPromotions.Add(new AppliedPromotionDto
        {
            PromotionId = promotion.Id,
            PromotionName = promotion.Name,
            PromotionRuleId = rule.Id,
            RuleType = ConvertRuleType(rule.RuleType),
            DiscountAmount = bundleDiscountAmount,
            Description = $"Bundle: {promotion.Name} for ${rule.FixedPrice.Value:F2}",
            AffectedProductIds = bundleItems.Select(i => i.ProductId).ToList()
        });

        return true;
    }

    private bool ApplyCouponRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, ApplyPromotionRulesDto applyDto, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        // Coupon validation is handled in FilterApplicablePromotions
        // This method can apply additional coupon-specific logic
        return ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result);
    }

    private bool ApplyTimeLimitedRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, ApplyPromotionRulesDto applyDto, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        // Time validation is handled in IsRuleApplicable
        // This method can apply additional time-specific logic
        return ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result);
    }

    private bool ApplyExclusiveRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        // Apply the discount and mark as exclusive
        return ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result);
    }

    private List<CartItemResultDto> GetRuleTargetItems(PromotionRule rule, List<CartItemResultDto> cartItems, HashSet<Guid> lockedLines)
    {
        var targetItems = cartItems.Where(item => !lockedLines.Contains(item.ProductId)).ToList();

        // Filter by specific products if rule specifies them
        if (rule.Products != null && rule.Products.Any())
        {
            var ruleProductIds = rule.Products.Select(p => p.ProductId).ToHashSet();
            targetItems = targetItems.Where(item => ruleProductIds.Contains(item.ProductId)).ToList();
        }

        // Filter by categories if rule specifies them
        if (rule.CategoryIds != null && rule.CategoryIds.Any())
        {
            targetItems = targetItems.Where(item =>
                item.CategoryIds != null &&
                item.CategoryIds.Any(catId => rule.CategoryIds.Contains(catId))).ToList();
        }

        return targetItems;
    }

    #endregion

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