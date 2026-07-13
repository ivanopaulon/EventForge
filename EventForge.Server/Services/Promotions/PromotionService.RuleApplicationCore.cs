using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
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
            var (filteredPromotions, exclusionMessages, nearMissPromotions) = FilterApplicablePromotionsWithMessages(applicablePromotions, applyDto);

            // Add exclusion messages to result
            result.Messages.AddRange(exclusionMessages);
            result.NearMissPromotions = nearMissPromotions;

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

                if (applied && promotion.Rules.Any(r => r.RuleType == EventForge.Server.Data.Entities.Promotions.PromotionRuleType.Exclusive))
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

}
