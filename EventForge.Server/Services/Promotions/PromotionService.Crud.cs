using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;


namespace EventForge.Server.Services.Promotions;

public partial class PromotionService
{
    public async Task<PagedResult<PromotionDto>> GetPromotionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        // NOTE: Tenant isolation test coverage should be expanded in future test iterations
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for promotion operations.");
        }

        var query = context.Promotions
            .AsNoTracking()
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

    public async Task<PromotionDto?> GetPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for promotion operations.");
        }

        var promotion = await context.Promotions
            .AsNoTracking()
            .Where(p => p.Id == id && p.TenantId == currentTenantId.Value && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (promotion is null)
        {
            logger.LogWarning("Promotion with ID {PromotionId} not found.", id);
            return null;
        }

        return MapToPromotionDto(promotion);
    }

    public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var promotions = await context.Promotions
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == Data.Entities.Promotions.PromotionStatus.Active)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return promotions.Select(MapToPromotionDto);
    }

    public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto, string currentUser, CancellationToken cancellationToken = default)
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

    public async Task<PromotionDto?> UpdatePromotionAsync(Guid id, UpdatePromotionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for promotion operations.");

        var originalPromotion = await context.Promotions
            .AsNoTracking()
            .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalPromotion is null)
        {
            logger.LogWarning("Promotion with ID {PromotionId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        var promotion = await context.Promotions
            .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
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
        promotion.Status = ConvertStatus(updateDto.Status);
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

    public async Task<bool> DeletePromotionAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for promotion operations.");

        var originalPromotion = await context.Promotions
            .AsNoTracking()
            .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalPromotion is null)
        {
            logger.LogWarning("Promotion with ID {PromotionId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        var promotion = await context.Promotions
            .Where(p => p.Id == id && p.TenantId == currentTenantId && !p.IsDeleted)
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

}
