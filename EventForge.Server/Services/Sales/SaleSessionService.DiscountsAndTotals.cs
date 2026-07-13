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
    public async Task<SaleSessionDto?> ApplyGlobalDiscountAsync(
        Guid sessionId,
        ApplyGlobalDiscountDto discountDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var session = await context.SaleSessions
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

        if (session is null)
        {
            return null;
        }

        // Validate session state
        if (session.Status == SaleSessionStatus.Closed)
        {
            throw new InvalidOperationException("Cannot apply discount to closed session");
        }

        // Apply discount to each item
        foreach (var item in session.Items)
        {
            item.ManualDiscountPercent = discountDto.DiscountPercent;
            item.DiscountPercent = item.ManualDiscountPercent + item.PromotionDiscountPercent;

            // Recalculate item totals
            var subtotal = item.UnitPrice * item.Quantity;
            var discountAmount = subtotal * (item.DiscountPercent / 100);
            var subtotalAfterDiscount = subtotal - discountAmount;
            item.TaxAmount = subtotalAfterDiscount * (item.TaxRate / 100);
            item.TotalAmount = subtotalAfterDiscount + item.TaxAmount;

            item.ModifiedBy = currentUser;
            item.ModifiedAt = DateTime.UtcNow;
        }

        // Recalculate session totals
        session.OriginalTotal = session.Items.Sum(i => i.UnitPrice * i.Quantity);
        session.DiscountAmount = session.Items.Sum(i => (i.UnitPrice * i.Quantity) * (i.DiscountPercent / 100));
        session.TaxAmount = session.Items.Sum(i => i.TaxAmount);
        session.FinalTotal = session.OriginalTotal - session.DiscountAmount + session.TaxAmount;

        session.ModifiedBy = currentUser;
        session.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Audit log
        await auditLogService.LogEntityChangeAsync(
            "SaleSession",
            session.Id,
            "DiscountAmount",
            "ApplyGlobalDiscount",
            null,
            $"{discountDto.DiscountPercent}% - {discountDto.Reason ?? "No reason"}",
            currentUser,
            "Sale Session",
            cancellationToken);

        logger.LogInformation(
            "Applied {DiscountPercent}% global discount to session {SessionId} by {User}",
            discountDto.DiscountPercent, sessionId, currentUser);

        return await MapToDtoAsync(session, cancellationToken);
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var session = await context.SaleSessions
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

        if (session is null)
        {
            return null;
        }

        await RecalculateTotalsAsync(session, cancellationToken);

        logger.LogInformation("Sale session {SessionId} totals recalculated.", sessionId);

        return await MapToDtoAsync(session, cancellationToken);
    }

}
