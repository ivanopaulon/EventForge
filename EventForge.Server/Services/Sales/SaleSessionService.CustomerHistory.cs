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

public partial class SaleSessionService
{
    public async Task<IEnumerable<Guid>> GetCustomerPurchasedProductIdsAsync(Guid customerId, int maxSessions = 30, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for sale session operations.");

        // Fetch the IDs of the most recent sessions for this customer directly from DB,
        // then collect distinct product IDs — no full SaleSessionDto hydration needed.
        var recentSessionIds = await context.SaleSessions
            .AsNoTracking()
            .Where(s => !s.IsDeleted
                && s.TenantId == currentTenantId.Value
                && s.CustomerId == customerId)
            .OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt)
            .Take(maxSessions)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (recentSessionIds.Count == 0)
            return Enumerable.Empty<Guid>();

        var productIds = await context.SaleItems
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                && i.TenantId == currentTenantId.Value
                && recentSessionIds.Contains(i.SaleSessionId))
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return productIds;
    }
}
