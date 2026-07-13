using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{

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
                EventForge.Server.Data.Entities.Promotions.PromotionRuleType.CustomerSpecific => ApplyDiscountRule(rule, cartItems, promotion, lockedLines, result),
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

}
