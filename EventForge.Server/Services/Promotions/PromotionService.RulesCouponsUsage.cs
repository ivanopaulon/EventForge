using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
    public async Task<IEnumerable<PromotionRuleDto>> GetApplicablePromotionRulesAsync(
        Guid? customerId = null,
        string? salesChannel = null,
        DateTime? orderDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var checkDateTime = orderDateTime ?? DateTime.UtcNow;

        var query = context.PromotionRules
            .AsNoTracking()
            .Include(pr => pr.Promotion)
            .Where(pr => !pr.IsDeleted &&
                       !pr.Promotion!.IsDeleted &&
                       pr.Promotion.Status == Data.Entities.Promotions.PromotionStatus.Active &&
                       pr.Promotion.StartDate <= checkDateTime &&
                       pr.Promotion.EndDate >= checkDateTime);

        // Apply filters based on parameters
        if (!string.IsNullOrEmpty(salesChannel) && salesChannel != "all")
        {
            query = query.Where(pr => pr.SalesChannels == null ||
                                    pr.SalesChannels.Contains(salesChannel));
        }

        var rules = await query.ToListAsync(cancellationToken);

        return rules.Select(MapToPromotionRuleDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<PromotionDto?> ValidateCouponAsync(string couponCode, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            logger.LogDebug("ValidateCouponAsync called with null or empty coupon code.");
            return null;
        }

        var now = DateTime.UtcNow;
        var normalizedCode = couponCode.Trim().ToUpperInvariant();

        var promotion = await context.Promotions
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive &&
                        p.Status == Data.Entities.Promotions.PromotionStatus.Active &&
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

    /// <inheritdoc/>
    public async Task<bool> IncrementUsageAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;

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
            PromotionType = ap.RuleType,
            Description = ap.Description
        }).ToList();

        return JsonSerializer.Serialize(snapshots, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

}
