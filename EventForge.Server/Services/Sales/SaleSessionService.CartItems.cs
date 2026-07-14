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
    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto addItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        // Validate session exists (no tracking to avoid modifying tracked SaleSession)
        var sessionExists = await context.SaleSessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

        if (!sessionExists)
        {
            return null;
        }

        // Load product (we need VAT and price info)
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.VatRate)
            .FirstOrDefaultAsync(p => p.Id == addItemDto.ProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken);

        if (product is null)
        {
            logger.LogWarning(
                "Product not found - ProductId: {ProductId}, TenantId: {TenantId}, SessionId: {SessionId}",
                addItemDto.ProductId,
                currentTenantId.Value,
                sessionId);
            throw new InvalidOperationException($"Product {addItemDto.ProductId} not found or not accessible.");
        }

        var unitPrice = addItemDto.UnitPrice ?? product.DefaultPrice ?? 0m;
        var subtotal = unitPrice * addItemDto.Quantity;
        var discountAmount = subtotal * (addItemDto.DiscountPercent / 100m);
        var totalAmount = subtotal - discountAmount;

        var taxRate = 0m;
        if (product.VatRateId.HasValue && product.VatRate is not null)
        {
            taxRate = product.VatRate.Percentage;
        }
        var taxAmount = totalAmount * (taxRate / 100m);

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId.Value,
            SaleSessionId = sessionId,
            ProductId = addItemDto.ProductId,
            ProductCode = product.Code,
            ProductName = product.Name,
            UnitPrice = unitPrice,
            Quantity = addItemDto.Quantity,
            DiscountPercent = addItemDto.DiscountPercent,
            TotalAmount = totalAmount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Notes = addItemDto.Notes,
            IsService = addItemDto.IsService,
            PriceListId = addItemDto.PriceListId,
            PriceListName = addItemDto.PriceListName,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };


        // Use explicit transaction: INSERT item via EF (tracked only for the item), then update session totals via raw SQL.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Insert the new sale item (tracked only as SaleItem)
            context.SaleItems.Add(item);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Inserted SaleItem {ItemId} for Session {SessionId}", item.Id, sessionId);

            // Recalculate and update totals using a single SQL statement to avoid loading/modifying the SaleSession entity
            var now = DateTime.UtcNow;

            // This uses parameterization via EF Core interpolated SQL to avoid SQL injection.
            await context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE ss
SET ss.OriginalTotal = COALESCE(s.OriginalTotal, 0),
    ss.DiscountAmount = COALESCE(s.DiscountAmount, 0),
    ss.TaxAmount = COALESCE(s.TaxAmount, 0),
    ss.FinalTotal = COALESCE(s.FinalTotal, 0),
    ss.ModifiedAt = {now},
    ss.ModifiedBy = {currentUser}
FROM SaleSessions ss
LEFT JOIN (
    SELECT si.SaleSessionId,
           SUM(si.UnitPrice * si.Quantity) AS OriginalTotal,
           SUM(si.TotalAmount) AS ItemsTotal,
           SUM(si.TaxAmount) AS TaxAmount,
           SUM(si.UnitPrice * si.Quantity) - SUM(si.TotalAmount) AS DiscountAmount,
           SUM(si.TotalAmount) + SUM(si.TaxAmount) AS FinalTotal
    FROM SaleItems si
    WHERE si.SaleSessionId = {sessionId} AND si.TenantId = {currentTenantId.Value} AND ISNULL(si.IsDeleted, 0) = 0
    GROUP BY si.SaleSessionId
) s ON s.SaleSessionId = ss.Id
WHERE ss.Id = {sessionId} AND ss.TenantId = {currentTenantId.Value};
", cancellationToken);

            // Commit SQL transaction (item insert + totals update)
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Session {SessionId} totals updated after adding item {ItemId}.", sessionId, item.Id);

            // Audit log: record that an item was added to the session
            _ = await auditLogService.LogEntityChangeAsync("SaleSession", sessionId, "Items", "AddItem", null, $"Added {product.Name}", currentUser, "Sale Session", cancellationToken);

            // Reload full session (with includes) to map and return DTO
            var reloadedSession = await context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (reloadedSession is null)
            {
                logger.LogWarning("Session {SessionId} not found after insert/update", sessionId);
                return null;
            }

            // Apply promotions to all items in the session and recalculate totals
            var nearMissPromotions = await ApplyPromotionsToSessionItemsAsync(reloadedSession, currentUser, cancellationToken);

            return await MapToDtoAsync(reloadedSession, cancellationToken, nearMissPromotions);
        }
        catch
        {
            // If something goes wrong, ensure rollback and rethrow
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
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

            var item = session.Items.FirstOrDefault(i => i.Id == itemId && !i.IsDeleted);
            if (item is null)
            {
                throw new InvalidOperationException($"Item {itemId} not found in session {sessionId}.");
            }

            item.Quantity = updateItemDto.Quantity;
            item.UnitPrice = updateItemDto.UnitPrice;
            item.DiscountPercent = updateItemDto.DiscountPercent;
            item.Notes = updateItemDto.Notes;
            item.PriceListId = updateItemDto.PriceListId;
            item.PriceListName = updateItemDto.PriceListName;

            var subtotal = item.UnitPrice * item.Quantity;
            var discountAmount = subtotal * (item.DiscountPercent / 100);
            item.TotalAmount = subtotal - discountAmount;
            item.TaxAmount = item.TotalAmount * (item.TaxRate / 100);

            item.ModifiedBy = currentUser;
            item.ModifiedAt = DateTime.UtcNow;

            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            await CalculateTotalsInlineAsync(session, cancellationToken);

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Single SaveChanges call to save both item and updated totals atomically
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "UpdateItem", item.Quantity.ToString(), updateItemDto.Quantity.ToString(), currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Updated item {ItemId} in sale session {SessionId}", itemId, sessionId);

            // Apply promotions to all items in the session after a manual update
            var nearMissPromotions = await ApplyPromotionsToSessionItemsAsync(session, currentUser, cancellationToken);

            return await MapToDtoAsync(session, cancellationToken, nearMissPromotions);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "DbUpdateConcurrencyException in UpdateItemAsync - SessionId: {SessionId}, ItemId: {ItemId}. The session or item was modified by another user.",
                sessionId,
                itemId);

            LogDetailedEntityStates(sessionId, tenantContext.CurrentTenantId ?? Guid.Empty);

            throw new InvalidOperationException(
                "The session or item was modified by another user. Please refresh and try again.", ex);
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
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

            var item = session.Items.FirstOrDefault(i => i.Id == itemId && !i.IsDeleted);
            if (item is null)
            {
                throw new InvalidOperationException($"Item {itemId} not found in session {sessionId}.");
            }

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.DeletedBy = currentUser;

            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            await CalculateTotalsInlineAsync(session, cancellationToken);

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Single SaveChanges call to save both item and updated totals atomically
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "RemoveItem", item.ProductName, "Removed", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Removed item {ItemId} from sale session {SessionId}", itemId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "DbUpdateConcurrencyException in RemoveItemAsync - SessionId: {SessionId}, ItemId: {ItemId}. The session or item was modified by another user.",
                sessionId,
                itemId);

            LogDetailedEntityStates(sessionId, tenantContext.CurrentTenantId ?? Guid.Empty);

            throw new InvalidOperationException(
                "The session or item was modified by another user. Please refresh and try again.", ex);
        }
    }

}
