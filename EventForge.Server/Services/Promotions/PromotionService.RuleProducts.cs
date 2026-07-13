using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
    public async Task<IEnumerable<PromotionRuleProductDto>> GetRuleProductsAsync(Guid promotionId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var products = await context.PromotionRuleProducts
            .AsNoTracking()
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

    public async Task<PromotionRuleProductDto> AddRuleProductAsync(Guid promotionId, Guid ruleId, CreatePromotionRuleProductDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");

        var rule = await context.PromotionRules
            .AsNoTracking()
            .Where(r => r.Id == ruleId && r.PromotionId == promotionId && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        if (rule is null)
            throw new InvalidOperationException($"Rule {ruleId} not found for promotion {promotionId}.");

        // Avoid duplicates
        var existing = await context.PromotionRuleProducts
            .AsNoTracking()
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
            .AsNoTracking()
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

    public async Task<bool> RemoveRuleProductAsync(Guid promotionId, Guid ruleId, Guid productId, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for promotion operations.");

        var ruleProduct = await context.PromotionRuleProducts
            .Where(rp => rp.PromotionRuleId == ruleId && rp.ProductId == productId && rp.TenantId == currentTenantId && !rp.IsDeleted &&
                         context.PromotionRules.Any(r => r.Id == ruleId && r.PromotionId == promotionId && r.TenantId == currentTenantId && !r.IsDeleted))
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

    public async Task<List<ProductPromotionMembershipDto>> GetPromotionsForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for promotion operations.");
        }

        var now = DateTime.UtcNow;

        var promotions = await context.Promotions
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value)
            .Include(p => p.Rules.Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value))
                .ThenInclude(r => r.Products.Where(rp => !rp.IsDeleted))
            .ToListAsync(cancellationToken);

        var result = new List<ProductPromotionMembershipDto>();

        foreach (var promotion in promotions)
        {
            // DECISIONE DI PRODOTTO: una regola senza prodotti espliciti (Products vuoto) si applica
            // a TUTTI i prodotti, quindi viene considerata "match" anche per il prodotto richiesto.
            // Questo è più corretto (evita falsi "non è in nessuna promozione" quando in realtà uno
            // sconto generico si applica comunque), a costo di un calcolo leggermente più oneroso.
            // Il DTO espone AppliesToAllProducts per distinguere in UI il targeting esplicito da quello generico.
            var matchingRule = promotion.Rules.FirstOrDefault(r =>
                r.Products.Count == 0 || r.Products.Any(rp => rp.ProductId == productId));

            if (matchingRule is null)
            {
                continue;
            }

            var appliesToAllProducts = matchingRule.Products.Count == 0;

            var isActive = promotion.Status == Data.Entities.Promotions.PromotionStatus.Active
                && promotion.StartDate <= now
                && promotion.EndDate >= now;

            result.Add(new ProductPromotionMembershipDto
            {
                PromotionId = promotion.Id,
                PromotionName = promotion.Name,
                IsActive = isActive,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                AppliesToAllProducts = appliesToAllProducts
            });
        }

        return result
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.PromotionName)
            .ToList();
    }
}
