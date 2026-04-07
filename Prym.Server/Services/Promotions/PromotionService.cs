using Prym.DTOs.Promotions;
using Prym.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Prym.Server.Services.Promotions;

/// <summary>
/// Service implementation for managing promotions.
/// </summary>
public class PromotionService(
    PrymDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<PromotionService> logger,
    IMemoryCache cache,
    IMonitoringMetricsService monitoringMetrics) : IPromotionService
{

    public async Task<PagedResult<PromotionDto>> GetPromotionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for promotion operations.");
            }

            var query = context.Promotions
                .WhereActiveTenant(currentTenantId.Value)
                .Include(p => p.Rules.Where(pr => !pr.IsDeleted && pr.TenantId == currentTenantId.Value));

            var totalCount = await query.CountAsync(cancellationToken);
            var promotions = await query
                .OrderBy(p => p.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var promotionDtos = promotions.Select(MapToPromotionDto);

            return new PagedResult<PromotionDto>
            {
                Items = promotionDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotions.");
            throw;
        }
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found.", id);
                return null;
            }

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var promotions = await context.Promotions
                .Where(p => !p.IsDeleted && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return promotions.Select(MapToPromotionDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active promotions.");
            throw;
        }
    }

    public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required.");

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createDto.Name,
                Description = createDto.Description,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                MinOrderAmount = createDto.MinOrderAmount,
                MaxUses = createDto.MaxUses,
                CouponCode = createDto.CouponCode,
                Priority = createDto.Priority,
                IsCombinable = createDto.IsCombinable,
                MaxTotalDiscountPercentage = createDto.MaxTotalDiscountPercentage,
                MaxUsesPerCustomer = createDto.MaxUsesPerCustomer,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _ = context.Promotions.Add(promotion);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Invalidate cache after creating promotion
            InvalidatePromotionCache();

            _ = await auditLogService.TrackEntityChangesAsync(promotion, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Promotion {PromotionId} created by {User}.", promotion.Id, currentUser);

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating promotion.");
            throw;
        }
    }

    public async Task<PromotionDto?> UpdatePromotionAsync(Guid id, UpdatePromotionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPromotion = await context.Promotions
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPromotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var promotion = await context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found for update by user {User}.", id, currentUser);
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
            promotion.MaxTotalDiscountPercentage = updateDto.MaxTotalDiscountPercentage;
            promotion.MaxUsesPerCustomer = updateDto.MaxUsesPerCustomer;
            promotion.ModifiedAt = DateTime.UtcNow;
            promotion.ModifiedBy = currentUser;

            // Apply optimistic concurrency: if client provided a RowVersion, use it as the
            // expected original value so EF Core detects concurrent modifications.
            if (updateDto.RowVersion is not null && updateDto.RowVersion.Length > 0)
                context.Entry(promotion).Property(p => p.RowVersion).OriginalValue = updateDto.RowVersion;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating promotion {PromotionId}.", id);
                throw new InvalidOperationException("La promozione è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Invalidate cache after updating promotion
            InvalidatePromotionCache();

            _ = await auditLogService.TrackEntityChangesAsync(promotion, "Update", currentUser, originalPromotion, cancellationToken);

            logger.LogInformation("Promotion {PromotionId} updated by {User}.", promotion.Id, currentUser);

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<bool> DeletePromotionAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPromotion = await context.Promotions
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPromotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var promotion = await context.Promotions
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            promotion.IsDeleted = true;
            promotion.DeletedAt = DateTime.UtcNow;
            promotion.DeletedBy = currentUser;
            promotion.ModifiedAt = DateTime.UtcNow;
            promotion.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            // Invalidate cache after deleting promotion
            InvalidatePromotionCache();

            _ = await auditLogService.TrackEntityChangesAsync(promotion, "Delete", currentUser, originalPromotion, cancellationToken);

            logger.LogInformation("Promotion {PromotionId} deleted by {User}.", promotion.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting promotion {PromotionId}.", id);
            throw;
        }
    }

    public async Task<bool> PromotionExistsAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Promotions
                .AnyAsync(p => p.Id == promotionId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if promotion {PromotionId} exists.", promotionId);
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
            CurrentUses = promotion.CurrentUses,
            CouponCode = promotion.CouponCode,
            Priority = promotion.Priority,
            IsCombinable = promotion.IsCombinable,
            MaxTotalDiscountPercentage = promotion.MaxTotalDiscountPercentage,
            MaxUsesPerCustomer = promotion.MaxUsesPerCustomer,
            CreatedAt = promotion.CreatedAt,
            CreatedBy = promotion.CreatedBy,
            ModifiedAt = promotion.ModifiedAt,
            ModifiedBy = promotion.ModifiedBy,
            RowVersion = promotion.RowVersion
        };
    }

    public async Task<PromotionApplicationResultDto> ApplyPromotionRulesAsync(ApplyPromotionRulesDto applyDto, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            logger.LogDebug("Starting promotion rule application for {ItemCount} cart items", applyDto.CartItems.Count);

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

            logger.LogDebug("Found {PromotionCount} applicable promotions", orderedPromotions.Count);

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
                FinalLineTotal = RoundCurrency(item.UnitPrice * item.Quantity * (1 - (item.ExistingLineDiscount / 100m))),
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

                if (applied && promotion.Rules.Any(r => r.RuleType == Prym.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive))
                {
                    exclusiveApplied = true;
                    result.Messages.Add($"Exclusive promotion '{promotion.Name}' applied - stopping further applications");
                }
            }

            // Step 7: Calculate final totals
            result.FinalTotal = result.CartItems.Sum(item => item.FinalLineTotal);
            result.TotalDiscountAmount = result.OriginalTotal - result.FinalTotal;

            // Enforce MaxTotalDiscountPercentage cap if set
            if (result.OriginalTotal > 0)
            {
                foreach (var promotion in orderedPromotions.Where(p => p.MaxTotalDiscountPercentage.HasValue))
                {
                    var maxDiscount = result.OriginalTotal * promotion.MaxTotalDiscountPercentage!.Value / 100m;
                    var currentDiscount = result.OriginalTotal - result.FinalTotal;
                    if (currentDiscount > maxDiscount)
                    {
                        var excessDiscount = currentDiscount - maxDiscount;
                        // Redistribute excess back to items proportionally
                        var eligibleItems = result.CartItems.Where(ci => ci.PromotionDiscount > 0).ToList();
                        var totalPromotionDiscount = eligibleItems.Sum(ci => ci.PromotionDiscount);
                        if (totalPromotionDiscount > 0)
                        {
                            foreach (var cartItem in eligibleItems)
                            {
                                var proportion = cartItem.PromotionDiscount / totalPromotionDiscount;
                                var reduction = RoundCurrency(excessDiscount * proportion);
                                cartItem.FinalLineTotal = RoundCurrency(cartItem.FinalLineTotal + reduction);
                                cartItem.PromotionDiscount = RoundCurrency(cartItem.PromotionDiscount - reduction);
                            }
                        }
                        result.FinalTotal = result.CartItems.Sum(item => item.FinalLineTotal);
                        result.TotalDiscountAmount = result.OriginalTotal - result.FinalTotal;
                        result.Messages.Add($"Discount capped at {promotion.MaxTotalDiscountPercentage.Value}% by promotion '{promotion.Name}'");
                    }
                }
            }

            logger.LogInformation("Promotion application completed. Original: {Original}, Final: {Final}, Discount: {Discount}",
                result.OriginalTotal, result.FinalTotal, result.TotalDiscountAmount);

            // Step 8: Increment usage counters for applied promotions (best-effort, non-blocking)
            var appliedPromotionIds = result.CartItems
                .SelectMany(ci => ci.AppliedPromotions)
                .Select(ap => ap.PromotionId)
                .Distinct()
                .ToList();

            foreach (var promotionId in appliedPromotionIds)
            {
                try
                {
                    var incremented = await IncrementUsageAsync(promotionId, cancellationToken);
                    if (!incremented)
                    {
                        logger.LogWarning("Could not increment usage for promotion {PromotionId} (MaxUses reached or not found).", promotionId);
                    }
                }
                catch (Exception ex)
                {
                    // Usage tracking failure must NOT block the order - log and continue
                    logger.LogError(ex, "Error incrementing usage for promotion {PromotionId}.", promotionId);
                }
            }

            sw.Stop();
            monitoringMetrics.RecordPricingOperation(true, sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            monitoringMetrics.RecordPricingOperation(false, sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Error applying promotion rules");
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
        if (applyDto.CartItems is null || !applyDto.CartItems.Any())
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
        if (applyDto.CouponCodes is not null)
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
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for promotion operations");
        }

        var cacheKey = $"ActivePromotions:{currentTenantId.Value}";

        if (cache.TryGetValue(cacheKey, out List<Promotion>? cachedPromotions))
        {
            logger.LogDebug("Retrieved {Count} promotions from cache", cachedPromotions?.Count ?? 0);
            return cachedPromotions ?? [];
        }

        logger.LogDebug("Cache miss - fetching promotions from database");

        var now = DateTime.UtcNow;
        var promotions = await context.Promotions
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
            Priority = CacheItemPriority.Normal,
            Size = 1
        };

        _ = cache.Set(cacheKey, promotions, cacheOptions);
        logger.LogDebug("Cached {Count} promotions", promotions.Count);

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
                    logger.LogDebug("Skipping promotion {PromotionName} - required coupon {Coupon} not provided",
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
                    logger.LogDebug("Skipping promotion {PromotionName} - minimum order amount {MinAmount} not met (current: {CurrentTotal})",
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
        // Check Business Party Groups (support both new and deprecated field)
#pragma warning disable CS0618
        var groupIdsToCheck = rule.BusinessPartyGroupIds ?? rule.CustomerGroupIds;
#pragma warning restore CS0618

        if (groupIdsToCheck is not null && groupIdsToCheck.Any())
        {
            if (applyDto.BusinessPartyGroupIds is null || !applyDto.BusinessPartyGroupIds.Any())
            {
                return false;
            }

            if (!groupIdsToCheck.Any(rg => applyDto.BusinessPartyGroupIds.Contains(rg)))
            {
                return false;
            }
        }

        // Check sales channel
        if (rule.SalesChannels is not null && rule.SalesChannels.Any() && !string.IsNullOrEmpty(applyDto.SalesChannel))
        {
            if (!rule.SalesChannels.Contains(applyDto.SalesChannel, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check time restrictions
        if (rule.ValidDays is not null && rule.ValidDays.Any())
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
    private static Prym.DTOs.Common.PromotionRuleType ConvertRuleType(Prym.Server.Data.Entities.Promotions.PromotionRuleType entityRuleType)
    {
        return entityRuleType switch
        {
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.Discount => Prym.DTOs.Common.PromotionRuleType.Discount,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.CategoryDiscount => Prym.DTOs.Common.PromotionRuleType.CategoryDiscount,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.CartAmountDiscount => Prym.DTOs.Common.PromotionRuleType.CartAmountDiscount,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.BuyXGetY => Prym.DTOs.Common.PromotionRuleType.BuyXGetY,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.FixedPrice => Prym.DTOs.Common.PromotionRuleType.FixedPrice,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.Bundle => Prym.DTOs.Common.PromotionRuleType.Bundle,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.CustomerSpecific => Prym.DTOs.Common.PromotionRuleType.CustomerSpecific,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.Coupon => Prym.DTOs.Common.PromotionRuleType.Coupon,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.TimeLimited => Prym.DTOs.Common.PromotionRuleType.TimeLimited,
            Prym.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive => Prym.DTOs.Common.PromotionRuleType.Exclusive,
            _ => Prym.DTOs.Common.PromotionRuleType.Discount
        };
    }

    /// <summary>
    /// Invalidates the promotion cache for the current tenant.
    /// </summary>
    private void InvalidatePromotionCache()
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (currentTenantId.HasValue)
        {
            var cacheKey = $"ActivePromotions:{currentTenantId.Value}";
            cache.Remove(cacheKey);
            logger.LogDebug("Invalidated promotion cache for tenant {TenantId}", currentTenantId.Value);
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
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.Discount => ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.CategoryDiscount => ApplyCategoryDiscountRule(rule, cartItems, promotion, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.CartAmountDiscount => ApplyCartAmountDiscountRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.BuyXGetY => ApplyBuyXGetYRule(rule, cartItems, promotion, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.FixedPrice => ApplyFixedPriceRule(rule, cartItems, promotion, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.Bundle => ApplyBundleRule(rule, cartItems, promotion, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.Coupon => ApplyCouponRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.TimeLimited => ApplyTimeLimitedRule(rule, cartItems, promotion, applyDto, lockedLines, result),
                Prym.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive => ApplyExclusiveRule(rule, cartItems, promotion, lockedLines, result),
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
                        _ = lockedLines.Add(item.ProductId);
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

                logger.LogDebug("Applied discount rule: {Description}, Amount: {Amount}", description, discountAmount);
            }
        }

        return applied;
    }

    private bool ApplyCategoryDiscountRule(PromotionRule rule, List<CartItemResultDto> cartItems, Promotion promotion, HashSet<Guid> lockedLines, PromotionApplicationResultDto result)
    {
        if (rule.CategoryIds is null || !rule.CategoryIds.Any())
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

                logger.LogDebug("Applied BuyXGetY rule: {Description}, Amount: {Amount}", description, discountAmount);
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
        if (!rule.FixedPrice.HasValue || rule.Products is null || rule.Products.Count < 2)
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
            if (matchingItem is null || matchingItem.Quantity < (requiredProduct.Quantity ?? 1))
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
        if (rule.Products is not null && rule.Products.Any())
        {
            var ruleProductIds = rule.Products.Select(p => p.ProductId).ToHashSet();
            targetItems = targetItems.Where(item => ruleProductIds.Contains(item.ProductId)).ToList();
        }

        // Filter by categories if rule specifies them
        if (rule.CategoryIds is not null && rule.CategoryIds.Any())
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

            var query = context.PromotionRules
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
            return rules.Select(MapToPromotionRuleDto).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving applicable promotion rules.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PromotionDto?> ValidateCouponAsync(string couponCode, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                logger.LogDebug("ValidateCouponAsync called with null or empty coupon code.");
                return null;
            }

            var now = DateTime.UtcNow;
            var normalizedCode = couponCode.Trim().ToUpperInvariant();

            var promotion = await context.Promotions
                .Where(p => !p.IsDeleted && p.IsActive &&
                            p.CouponCode != null &&
                            p.CouponCode.ToUpper() == normalizedCode &&
                            p.StartDate <= now && p.EndDate >= now)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
            {
                logger.LogInformation("Coupon code '{CouponCode}' is invalid, expired, or not found.", couponCode);
                return null;
            }

            if (promotion.MaxUses.HasValue && promotion.CurrentUses >= promotion.MaxUses.Value)
            {
                logger.LogInformation("Coupon code '{CouponCode}' has reached its maximum uses limit ({MaxUses}).", couponCode, promotion.MaxUses.Value);
                return null;
            }

            logger.LogInformation("Coupon code '{CouponCode}' validated successfully for promotion '{PromotionName}' ({PromotionId}).", couponCode, promotion.Name, promotion.Id);

            _ = await auditLogService.TrackEntityChangesAsync(promotion, "ValidateCoupon", "System", null, cancellationToken);

            return MapToPromotionDto(promotion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating coupon code '{CouponCode}'.", couponCode);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementUsageAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;

        try
        {
            var promotion = await context.Promotions
                .Where(p => p.Id == promotionId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
            {
                logger.LogWarning("Promotion with ID {PromotionId} not found for usage increment.", promotionId);
                return false;
            }

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (promotion.MaxUses.HasValue && promotion.CurrentUses >= promotion.MaxUses.Value)
                {
                    logger.LogInformation("Promotion {PromotionId} has reached its maximum uses limit ({MaxUses}).", promotionId, promotion.MaxUses.Value);
                    return false;
                }

                promotion.CurrentUses++;

                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    _ = await auditLogService.TrackEntityChangesAsync(promotion, "IncrementUsage", "System", null, cancellationToken);
                    logger.LogInformation("Successfully incremented usage for promotion {PromotionId}. CurrentUses: {CurrentUses}.", promotionId, promotion.CurrentUses);
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    logger.LogWarning(ex, "Concurrency conflict while incrementing usage for promotion {PromotionId}. Attempt {Attempt}/{MaxRetries}.", promotionId, attempt + 1, maxRetries);

                    if (attempt >= maxRetries - 1)
                    {
                        logger.LogError(ex, "Failed to increment usage for promotion {PromotionId} after {MaxRetries} attempts.", promotionId, maxRetries);
                        throw;
                    }

                    // Exponential backoff with cap: 50ms, 100ms, 200ms, … up to 500ms
                    var delayMs = Math.Min(50 * (1 << attempt), 500);
                    await Task.Delay(delayMs, cancellationToken);

                    var entry = ex.Entries.FirstOrDefault();
                    if (entry is not null)
                    {
                        await entry.ReloadAsync(cancellationToken);
                        promotion = (Promotion)entry.Entity;
                    }
                }
            }

            // Unreachable in practice: the loop always returns true, returns false on limit reached,
            // or throws on exhausted retries; required to satisfy the compiler.
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing usage for promotion {PromotionId}.", promotionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public string? SerializeAppliedPromotionsJson(IEnumerable<AppliedPromotionDto> appliedPromotions)
    {
        var list = appliedPromotions?.ToList();
        if (list is null || list.Count == 0)
        {
            return null;
        }

        var snapshots = list.Select(ap => new AppliedPromotionSnapshot
        {
            PromotionId = ap.PromotionId,
            PromotionName = ap.PromotionName,
            DiscountAmount = ap.DiscountAmount,
            DiscountPercentage = ap.DiscountPercentage,
            PromotionType = ap.RuleType
        }).ToList();

        return JsonSerializer.Serialize(snapshots, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<IEnumerable<PromotionRuleDto>> GetPromotionRulesAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await context.PromotionRules
                .Include(r => r.Products.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Product)
                .Where(r => r.PromotionId == promotionId && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            return rules.Select(MapToPromotionRuleDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving rules for promotion {PromotionId}.", promotionId);
            throw;
        }
    }

    public async Task<PromotionRuleDto> AddPromotionRuleAsync(Guid promotionId, CreatePromotionRuleDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required.");

            var promotion = await context.Promotions
                .Where(p => p.Id == promotionId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion is null)
                throw new InvalidOperationException($"Promotion {promotionId} not found.");

            if (!Enum.TryParse<Data.Entities.Promotions.PromotionRuleType>(createDto.RuleType, true, out var ruleType))
                throw new InvalidOperationException($"Invalid rule type: {createDto.RuleType}");

            var rule = new Data.Entities.Promotions.PromotionRule
            {
                Id = Guid.NewGuid(),
                PromotionId = promotionId,
                TenantId = currentTenantId.Value,
                RuleType = ruleType,
                DiscountPercentage = createDto.DiscountPercentage,
                DiscountAmount = createDto.DiscountAmount,
                RequiredQuantity = createDto.RequiredQuantity,
                FreeQuantity = createDto.FreeQuantity,
                FixedPrice = createDto.FixedPrice,
                MinOrderAmount = createDto.MinOrderAmount,
                IsCombinable = createDto.IsCombinable,
                CategoryIds = createDto.CategoryIds,
                BusinessPartyGroupIds = createDto.BusinessPartyGroupIds,
                SalesChannels = createDto.SalesChannels,
                ValidDays = createDto.ValidDays,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            context.PromotionRules.Add(rule);
            await context.SaveChangesAsync(cancellationToken);

            // Add product associations
            if (createDto.ProductIds is not null && createDto.ProductIds.Any())
            {
                foreach (var productId in createDto.ProductIds.Distinct())
                {
                    context.PromotionRuleProducts.Add(new Data.Entities.Promotions.PromotionRuleProduct
                    {
                        Id = Guid.NewGuid(),
                        PromotionRuleId = rule.Id,
                        ProductId = productId,
                        TenantId = currentTenantId.Value,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    });
                }
                await context.SaveChangesAsync(cancellationToken);
            }

            InvalidatePromotionCache();
            logger.LogInformation("Rule {RuleId} added to promotion {PromotionId} by {User}.", rule.Id, promotionId, currentUser);

            return MapToPromotionRuleDto(rule);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding rule to promotion {PromotionId}.", promotionId);
            throw;
        }
    }

    public async Task<PromotionRuleDto?> UpdatePromotionRuleAsync(Guid promotionId, Guid ruleId, UpdatePromotionRuleDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var rule = await context.PromotionRules
                .Where(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (rule is null)
            {
                logger.LogWarning("Rule {RuleId} for promotion {PromotionId} not found.", ruleId, promotionId);
                return null;
            }

            if (!Enum.TryParse<Data.Entities.Promotions.PromotionRuleType>(updateDto.RuleType, true, out var ruleType))
                throw new InvalidOperationException($"Invalid rule type: {updateDto.RuleType}");

            rule.RuleType = ruleType;
            rule.DiscountPercentage = updateDto.DiscountPercentage;
            rule.DiscountAmount = updateDto.DiscountAmount;
            rule.RequiredQuantity = updateDto.RequiredQuantity;
            rule.FreeQuantity = updateDto.FreeQuantity;
            rule.FixedPrice = updateDto.FixedPrice;
            rule.MinOrderAmount = updateDto.MinOrderAmount;
            rule.IsCombinable = updateDto.IsCombinable;
            rule.CategoryIds = updateDto.CategoryIds;
            rule.BusinessPartyGroupIds = updateDto.BusinessPartyGroupIds;
            rule.SalesChannels = updateDto.SalesChannels;
            rule.ValidDays = updateDto.ValidDays;
            rule.StartTime = updateDto.StartTime;
            rule.EndTime = updateDto.EndTime;
            rule.ModifiedAt = DateTime.UtcNow;
            rule.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);

            // Update product associations if provided
            if (updateDto.ProductIds is not null)
            {
                var existingProducts = await context.PromotionRuleProducts
                    .Where(rp => rp.PromotionRuleId == ruleId && !rp.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var ep in existingProducts)
                {
                    ep.IsDeleted = true;
                    ep.ModifiedAt = DateTime.UtcNow;
                    ep.ModifiedBy = currentUser;
                }

                foreach (var productId in updateDto.ProductIds.Distinct())
                {
                    context.PromotionRuleProducts.Add(new Data.Entities.Promotions.PromotionRuleProduct
                    {
                        Id = Guid.NewGuid(),
                        PromotionRuleId = ruleId,
                        ProductId = productId,
                        TenantId = rule.TenantId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    });
                }
                await context.SaveChangesAsync(cancellationToken);
            }

            InvalidatePromotionCache();
            logger.LogInformation("Rule {RuleId} for promotion {PromotionId} updated by {User}.", ruleId, promotionId, currentUser);

            return MapToPromotionRuleDto(rule);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating rule {RuleId} for promotion {PromotionId}.", ruleId, promotionId);
            throw;
        }
    }

    public async Task<bool> DeletePromotionRuleAsync(Guid promotionId, Guid ruleId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var rule = await context.PromotionRules
                .Where(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (rule is null)
            {
                logger.LogWarning("Rule {RuleId} for promotion {PromotionId} not found.", ruleId, promotionId);
                return false;
            }

            rule.IsDeleted = true;
            rule.ModifiedAt = DateTime.UtcNow;
            rule.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);

            InvalidatePromotionCache();
            logger.LogInformation("Rule {RuleId} for promotion {PromotionId} deleted by {User}.", ruleId, promotionId, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting rule {RuleId} for promotion {PromotionId}.", ruleId, promotionId);
            throw;
        }
    }

    private static PromotionRuleDto MapToPromotionRuleDto(Data.Entities.Promotions.PromotionRule rule)
    {
        return new PromotionRuleDto
        {
            Id = rule.Id,
            PromotionId = rule.PromotionId,
            RuleType = rule.RuleType.ToString(),
            DiscountPercentage = rule.DiscountPercentage,
            DiscountAmount = rule.DiscountAmount,
            RequiredQuantity = rule.RequiredQuantity,
            FreeQuantity = rule.FreeQuantity,
            FixedPrice = rule.FixedPrice,
            MinOrderAmount = rule.MinOrderAmount,
            IsCombinable = rule.IsCombinable,
            Products = rule.Products
                .Where(p => !p.IsDeleted)
                .Select(p => new PromotionRuleProductDto
                {
                    Id = p.Id,
                    PromotionRuleId = p.PromotionRuleId,
                    ProductId = p.ProductId,
                    ProductName = p.Product?.Name,
                    ProductCode = p.Product?.Code,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy,
                    ModifiedAt = p.ModifiedAt,
                    ModifiedBy = p.ModifiedBy
                }).ToList(),
            CategoryIds = rule.CategoryIds,
            BusinessPartyGroupIds = rule.BusinessPartyGroupIds,
            SalesChannels = rule.SalesChannels,
            ValidDays = rule.ValidDays,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime,
            CreatedAt = rule.CreatedAt,
            CreatedBy = rule.CreatedBy,
            ModifiedAt = rule.ModifiedAt,
            ModifiedBy = rule.ModifiedBy
        };
    }

    public async Task<IEnumerable<PromotionRuleProductDto>> GetRuleProductsAsync(Guid promotionId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await context.PromotionRuleProducts
                .Include(rp => rp.Product)
                .Where(rp => rp.PromotionRuleId == ruleId && !rp.IsDeleted &&
                             context.PromotionRules.Any(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted))
                .OrderBy(rp => rp.CreatedAt)
                .ToListAsync(cancellationToken);

            return products.Select(p => new PromotionRuleProductDto
            {
                Id = p.Id,
                PromotionRuleId = p.PromotionRuleId,
                ProductId = p.ProductId,
                ProductName = p.Product?.Name,
                ProductCode = p.Product?.Code,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                ModifiedAt = p.ModifiedAt,
                ModifiedBy = p.ModifiedBy
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products for rule {RuleId}.", ruleId);
            throw;
        }
    }

    public async Task<PromotionRuleProductDto> AddRuleProductAsync(Guid promotionId, Guid ruleId, CreatePromotionRuleProductDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
                throw new InvalidOperationException("Tenant context is required.");

            var rule = await context.PromotionRules
                .Where(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (rule is null)
                throw new InvalidOperationException($"Rule {ruleId} not found for promotion {promotionId}.");

            // Avoid duplicates
            var existing = await context.PromotionRuleProducts
                .FirstOrDefaultAsync(rp => rp.PromotionRuleId == ruleId && rp.ProductId == createDto.ProductId && !rp.IsDeleted, cancellationToken);
            if (existing is not null)
                return new PromotionRuleProductDto
                {
                    Id = existing.Id,
                    PromotionRuleId = existing.PromotionRuleId,
                    ProductId = existing.ProductId,
                    CreatedAt = existing.CreatedAt,
                    CreatedBy = existing.CreatedBy
                };

            var ruleProduct = new Data.Entities.Promotions.PromotionRuleProduct
            {
                Id = Guid.NewGuid(),
                PromotionRuleId = ruleId,
                ProductId = createDto.ProductId,
                TenantId = currentTenantId.Value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            context.PromotionRuleProducts.Add(ruleProduct);
            await context.SaveChangesAsync(cancellationToken);
            InvalidatePromotionCache();

            var product = await context.Set<Data.Entities.Products.Product>()
                .Where(p => p.Id == createDto.ProductId)
                .FirstOrDefaultAsync(cancellationToken);

            return new PromotionRuleProductDto
            {
                Id = ruleProduct.Id,
                PromotionRuleId = ruleProduct.PromotionRuleId,
                ProductId = ruleProduct.ProductId,
                ProductName = product?.Name,
                ProductCode = product?.Code,
                CreatedAt = ruleProduct.CreatedAt,
                CreatedBy = ruleProduct.CreatedBy
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding product {ProductId} to rule {RuleId}.", createDto?.ProductId, ruleId);
            throw;
        }
    }

    public async Task<bool> RemoveRuleProductAsync(Guid promotionId, Guid ruleId, Guid productId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var ruleProduct = await context.PromotionRuleProducts
                .Where(rp => rp.PromotionRuleId == ruleId && rp.ProductId == productId && !rp.IsDeleted &&
                             context.PromotionRules.Any(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (ruleProduct is null)
                return false;

            ruleProduct.IsDeleted = true;
            ruleProduct.ModifiedAt = DateTime.UtcNow;
            ruleProduct.ModifiedBy = currentUser;
            await context.SaveChangesAsync(cancellationToken);
            InvalidatePromotionCache();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product {ProductId} from rule {RuleId}.", productId, ruleId);
            throw;
        }
    }

}
