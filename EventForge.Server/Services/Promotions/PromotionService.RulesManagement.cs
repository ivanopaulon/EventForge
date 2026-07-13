using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
    public async Task<IEnumerable<PromotionRuleDto>> GetPromotionRulesAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var rules = await context.PromotionRules
            .AsNoTracking()
            .Include(r => r.Products.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Product)
            .Where(r => r.PromotionId == promotionId && !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToPromotionRuleDto);
    }

    public async Task<PromotionRuleDto> AddPromotionRuleAsync(Guid promotionId, CreatePromotionRuleDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");

        var promotion = await context.Promotions
            .AsNoTracking()
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

    public async Task<PromotionRuleDto?> UpdatePromotionRuleAsync(Guid promotionId, Guid ruleId, UpdatePromotionRuleDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for promotion operations.");

        var rule = await context.PromotionRules
            .Where(r => r.Id == ruleId && r.PromotionId == promotionId && r.TenantId == currentTenantId && !r.IsDeleted)
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

    public async Task<bool> DeletePromotionRuleAsync(Guid promotionId, Guid ruleId, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for promotion operations.");

        var rule = await context.PromotionRules
            .Where(r => r.Id == ruleId && r.PromotionId == promotionId && r.TenantId == currentTenantId && !r.IsDeleted)
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

}
