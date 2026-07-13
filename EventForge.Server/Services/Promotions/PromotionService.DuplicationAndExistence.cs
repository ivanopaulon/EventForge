using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
    public async Task<DuplicatePromotionResultDto> DuplicatePromotionAsync(Guid promotionId, DuplicatePromotionDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");

        var source = await context.Promotions
            .AsNoTracking()
            .Include(p => p.Rules.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Products)
            .Where(p => p.Id == promotionId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
            throw new InvalidOperationException($"Promotion {promotionId} not found.");

        var newPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId.Value,
            Name = dto.Name,
            Description = source.Description,
            StartDate = dto.NewStartDate ?? source.StartDate,
            EndDate = dto.NewEndDate ?? source.EndDate,
            MinOrderAmount = source.MinOrderAmount,
            MaxUses = source.MaxUses,
            CouponCode = dto.CouponCode,
            Priority = source.Priority,
            IsCombinable = source.IsCombinable,
            MaxTotalDiscountPercentage = source.MaxTotalDiscountPercentage,
            MaxUsesPerCustomer = source.MaxUsesPerCustomer,
            CurrentUses = 0,
            Status = Data.Entities.Promotions.PromotionStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            IsActive = true
        };

        context.Promotions.Add(newPromotion);
        await context.SaveChangesAsync(cancellationToken);

        var rulesCopied = 0;
        if (dto.CopyRules)
        {
            foreach (var rule in source.Rules)
            {
                var newRule = new Data.Entities.Promotions.PromotionRule
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId.Value,
                    PromotionId = newPromotion.Id,
                    RuleType = rule.RuleType,
                    DiscountPercentage = rule.DiscountPercentage,
                    DiscountAmount = rule.DiscountAmount,
                    RequiredQuantity = rule.RequiredQuantity,
                    FreeQuantity = rule.FreeQuantity,
                    FixedPrice = rule.FixedPrice,
                    MinOrderAmount = rule.MinOrderAmount,
                    CategoryIds = rule.CategoryIds,
                    BusinessPartyGroupIds = rule.BusinessPartyGroupIds,
                    SalesChannels = rule.SalesChannels,
                    ValidDays = rule.ValidDays,
                    StartTime = rule.StartTime,
                    EndTime = rule.EndTime,
                    IsCombinable = rule.IsCombinable,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser
                };
                context.PromotionRules.Add(newRule);
                await context.SaveChangesAsync(cancellationToken);

                foreach (var product in rule.Products)
                {
                    context.PromotionRuleProducts.Add(new Data.Entities.Promotions.PromotionRuleProduct
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId.Value,
                        PromotionRuleId = newRule.Id,
                        ProductId = product.ProductId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    });
                }
                if (rule.Products.Any())
                    await context.SaveChangesAsync(cancellationToken);

                rulesCopied++;
            }
        }

        InvalidatePromotionCache();
        logger.LogInformation("Promotion {SourceId} duplicated as {NewId} with {RuleCount} rules by {User}.",
            promotionId, newPromotion.Id, rulesCopied, currentUser);

        return new DuplicatePromotionResultDto
        {
            NewPromotion = MapToPromotionDto(newPromotion),
            RulesCopied = rulesCopied
        };
    }

    public async Task<bool> PromotionExistsAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        return await context.Promotions
            .AsNoTracking()
            .AnyAsync(p => p.Id == promotionId && !p.IsDeleted, cancellationToken);
    }

}
