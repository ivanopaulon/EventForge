using EventForge.Server.Services.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Promotions;
using System.Text.Json;

namespace EventForge.Server.Services.Promotions;

/// <summary>
/// Service implementation for managing promotions.
/// </summary>
public partial class PromotionService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<PromotionService> logger,
    IMemoryCache cache,
    IMonitoringMetricsService monitoringMetrics) : IPromotionService
{

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
            Status = ConvertStatus(promotion.Status),
            CreatedAt = promotion.CreatedAt,
            CreatedBy = promotion.CreatedBy,
            ModifiedAt = promotion.ModifiedAt,
            ModifiedBy = promotion.ModifiedBy,
            RowVersion = promotion.RowVersion
        };
    }

    /// <summary>
    /// Converts entity PromotionStatus to DTO PromotionStatus.
    /// </summary>
    private static Prym.DTOs.Common.PromotionStatus ConvertStatus(Data.Entities.Promotions.PromotionStatus entityStatus) => entityStatus switch
    {
        Data.Entities.Promotions.PromotionStatus.Draft => Prym.DTOs.Common.PromotionStatus.Draft,
        Data.Entities.Promotions.PromotionStatus.Active => Prym.DTOs.Common.PromotionStatus.Active,
        Data.Entities.Promotions.PromotionStatus.Suspended => Prym.DTOs.Common.PromotionStatus.Suspended,
        Data.Entities.Promotions.PromotionStatus.Archived => Prym.DTOs.Common.PromotionStatus.Archived,
        _ => Prym.DTOs.Common.PromotionStatus.Active
    };

    /// <summary>
    /// Converts DTO PromotionStatus to entity PromotionStatus.
    /// </summary>
    private static Data.Entities.Promotions.PromotionStatus ConvertStatus(Prym.DTOs.Common.PromotionStatus dtoStatus) => dtoStatus switch
    {
        Prym.DTOs.Common.PromotionStatus.Draft => Data.Entities.Promotions.PromotionStatus.Draft,
        Prym.DTOs.Common.PromotionStatus.Active => Data.Entities.Promotions.PromotionStatus.Active,
        Prym.DTOs.Common.PromotionStatus.Suspended => Data.Entities.Promotions.PromotionStatus.Suspended,
        Prym.DTOs.Common.PromotionStatus.Archived => Data.Entities.Promotions.PromotionStatus.Archived,
        _ => Data.Entities.Promotions.PromotionStatus.Active
    };

}
