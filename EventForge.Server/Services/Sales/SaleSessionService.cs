using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;
using Prym.DTOs.Promotions;
using Prym.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing sales sessions.
/// </summary>
public partial class SaleSessionService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<SaleSessionService> logger,
    IDocumentHeaderService documentHeaderService,
    IStockMovementService stockMovementService,
    IPromotionService promotionService,
    IFiscalDrawerService fiscalDrawerService) : ISaleSessionService
{

    /// <summary>
    /// Tolerance for percentage sum validation (allows for rounding errors).
    /// </summary>
    private const decimal PercentageSumTolerance = 0.01m;

}
