using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{

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
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value)
            .Where(p => p.Status == Data.Entities.Promotions.PromotionStatus.Active)
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
    private (List<Promotion>, List<string>, List<PromotionNearMissDto>) FilterApplicablePromotionsWithMessages(List<Promotion> promotions, ApplyPromotionRulesDto applyDto)
    {
        var filtered = new List<Promotion>();
        var exclusionMessages = new List<string>();
        var nearMissPromotions = new List<PromotionNearMissDto>();
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
                    nearMissPromotions.Add(new PromotionNearMissDto
                    {
                        PromotionId = promotion.Id,
                        PromotionName = promotion.Name,
                        CurrentTotal = currentTotal,
                        RequiredAmount = promotion.MinOrderAmount.Value
                    });
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

        return (filtered, exclusionMessages, nearMissPromotions);
    }

    /// <summary>
    /// Filters promotions based on the apply context.
    /// </summary>
    private List<Promotion> FilterApplicablePromotions(List<Promotion> promotions, ApplyPromotionRulesDto applyDto)
    {
        var (filtered, _, _) = FilterApplicablePromotionsWithMessages(promotions, applyDto);
        return filtered;
    }

    /// <summary>
    /// Checks if a promotion rule is applicable to the current context.
    /// </summary>
    private bool IsRuleApplicable(PromotionRule rule, ApplyPromotionRulesDto applyDto)
    {
        // Check Business Party Groups
        var groupIdsToCheck = rule.BusinessPartyGroupIds;

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
    private static Prym.DTOs.Common.PromotionRuleType ConvertRuleType(EventForge.Server.Data.Entities.Promotions.PromotionRuleType entityRuleType)
    {
        return entityRuleType switch
        {
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Discount => Prym.DTOs.Common.PromotionRuleType.Discount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CategoryDiscount => Prym.DTOs.Common.PromotionRuleType.CategoryDiscount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CartAmountDiscount => Prym.DTOs.Common.PromotionRuleType.CartAmountDiscount,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.BuyXGetY => Prym.DTOs.Common.PromotionRuleType.BuyXGetY,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.FixedPrice => Prym.DTOs.Common.PromotionRuleType.FixedPrice,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Bundle => Prym.DTOs.Common.PromotionRuleType.Bundle,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CustomerSpecific => Prym.DTOs.Common.PromotionRuleType.CustomerSpecific,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Coupon => Prym.DTOs.Common.PromotionRuleType.Coupon,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.TimeLimited => Prym.DTOs.Common.PromotionRuleType.TimeLimited,
            EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive => Prym.DTOs.Common.PromotionRuleType.Exclusive,
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

}
