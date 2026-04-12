using EventForge.DTOs.Documents;
using EventForge.DTOs.Promotions;
using EventForge.DTOs.Sales;
using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing sales sessions.
/// </summary>
public class SaleSessionService(
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

    public async Task<SaleSessionDto> CreateSessionAsync(CreateSaleSessionDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = new SaleSession
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                OperatorId = createDto.OperatorId,
                PosId = createDto.PosId,
                CustomerId = createDto.CustomerId,
                SaleType = createDto.SaleType,
                TableId = createDto.TableId,
                Currency = createDto.Currency,
                Status = SaleSessionStatus.Open,
                OriginalTotal = 0,
                DiscountAmount = 0,
                FinalTotal = 0,
                TaxAmount = 0,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _ = context.SaleSessions.Add(session);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Create", null, "Open", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Created sale session {SessionId} for operator {OperatorId} at POS {PosId}", session.Id, createDto.OperatorId, createDto.PosId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
            {
                return null;
            }

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
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

            session.CustomerId = updateDto.CustomerId ?? session.CustomerId;
            session.SaleType = updateDto.SaleType ?? session.SaleType;

            if (updateDto.Status.HasValue)
            {
                session.Status = (SaleSessionStatus)updateDto.Status.Value;
            }

            if (updateDto.CouponCodes is not null)
            {
                session.CouponCodes = updateDto.CouponCodes.Count > 0
                    ? string.Join(",", updateDto.CouponCodes.Select(c => c.Trim().ToUpperInvariant()))
                    : null;
            }

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Update", null, session.Status.ToString(), currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Updated sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await context.SaleSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
            {
                return false;
            }

            session.IsDeleted = true;
            session.DeletedAt = DateTime.UtcNow;
            session.DeletedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "IsDeleted", "Delete", "false", "true", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Deleted sale session {SessionId}", sessionId);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto addItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
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
                await ApplyPromotionsToSessionItemsAsync(reloadedSession, currentUser, cancellationToken);

                return await MapToDtoAsync(reloadedSession, cancellationToken);
            }
            catch
            {
                // If something goes wrong, ensure rollback and rethrow
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
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

            var subtotal = item.UnitPrice * item.Quantity;
            var discountAmount = subtotal * (item.DiscountPercent / 100);
            item.TotalAmount = subtotal - discountAmount;
            item.TaxAmount = item.TotalAmount * (item.TaxRate / 100);

            item.ModifiedBy = currentUser;
            item.ModifiedAt = DateTime.UtcNow;

            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            CalculateTotalsInline(session);

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Single SaveChanges call to save both item and updated totals atomically
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "UpdateItem", item.Quantity.ToString(), updateItemDto.Quantity.ToString(), currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Updated item {ItemId} in sale session {SessionId}", itemId, sessionId);

            // Apply promotions to all items in the session after a manual update
            await ApplyPromotionsToSessionItemsAsync(session, currentUser, cancellationToken);

            return await MapToDtoAsync(session, cancellationToken);
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
        catch (Exception ex)
        {
            throw;
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
            CalculateTotalsInline(session);

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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto addPaymentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            // Verify session exists (no tracking to avoid modifying SaleSession row)
            var sessionExists = await context.SaleSessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (!sessionExists)
            {
                return null;
            }

            var payment = new SalePayment
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                PaymentMethodId = addPaymentDto.PaymentMethodId,
                Amount = addPaymentDto.Amount,
                Status = Data.Entities.Sales.PaymentStatus.Completed,
                Notes = addPaymentDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            // Insert payment only (avoid touching SaleSession entity)
            context.SalePayments.Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", sessionId, "Payments", "AddPayment", null, $"Payment: {addPaymentDto.Amount}", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Inserted payment {PaymentId} of {Amount} for sale session {SessionId}", payment.Id, payment.Amount, sessionId);

            // Reload full session with includes to return a consistent DTO
            var reloadedSession = await context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (reloadedSession is null)
            {
                logger.LogWarning("Session {SessionId} not found after inserting payment {PaymentId}", sessionId, payment.Id);
                return null;
            }

            return await MapToDtoAsync(reloadedSession, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, string currentUser, CancellationToken cancellationToken = default)
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

            var payment = session.Payments.FirstOrDefault(p => p.Id == paymentId && !p.IsDeleted);
            if (payment is null)
            {
                throw new InvalidOperationException($"Payment {paymentId} not found in session {sessionId}.");
            }

            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
            payment.DeletedBy = currentUser;

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Payments", "RemovePayment", payment.Amount.ToString(), "Removed", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Removed payment {PaymentId} from sale session {SessionId}", paymentId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto addNoteDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            // Get user ID from username BEFORE loading session to avoid DataReader conflicts
            var userId = await GetUserIdFromUsernameAsync(currentUser, cancellationToken);

            var session = await context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
            {
                return null;
            }

            var note = new SessionNote
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                NoteFlagId = addNoteDto.NoteFlagId,
                Text = addNoteDto.Text,
                CreatedByUserId = userId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            session.Notes.Add(note);
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Notes", "AddNote", null, "Note added", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Added note to sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> ApplyGlobalDiscountAsync(
        Guid sessionId,
        ApplyGlobalDiscountDto discountDto,
        string currentUser,
        CancellationToken cancellationToken = default)
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

            // Validate session state
            if (session.Status == SaleSessionStatus.Closed)
            {
                throw new InvalidOperationException("Cannot apply discount to closed session");
            }

            // Apply discount to each item
            foreach (var item in session.Items)
            {
                item.DiscountPercent = discountDto.DiscountPercent;

                // Recalculate item totals
                var subtotal = item.UnitPrice * item.Quantity;
                var discountAmount = subtotal * (discountDto.DiscountPercent / 100);
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
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

            await RecalculateTotalsAsync(session, cancellationToken);

            logger.LogInformation("Sale session {SessionId} totals recalculated.", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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
                .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
            {
                return null;
            }

            // Validate that session is fully paid
            var completedPayments = session.Payments.Where(p => !p.IsDeleted && p.Status == Data.Entities.Sales.PaymentStatus.Completed).Sum(p => p.Amount);
            if (completedPayments < session.FinalTotal)
            {
                throw new InvalidOperationException($"Session cannot be closed. Total paid ({completedPayments}) is less than final total ({session.FinalTotal}).");
            }

            // No manual transaction needed - EF Core will use implicit transaction via SaveChangesAsync
            // This prevents nested transaction errors when GenerateDocumentNumberAsync is called
            session.Status = SaleSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Generate document
            var documentId = await GenerateReceiptDocumentAsync(session, currentUser, cancellationToken);

            if (documentId.HasValue)
            {
                session.DocumentId = documentId.Value;
            }

            // SaveChanges will handle transaction atomicity
            await context.SaveChangesAsync(cancellationToken);

            // Update fiscal drawer session totals (best-effort — session is already closed)
            try
            {
                if (session.PosId != Guid.Empty)
                {
                    var drawer = await fiscalDrawerService.GetFiscalDrawerByPosIdAsync(session.PosId, cancellationToken);
                    if (drawer is not null)
                    {
                        var paidItems = session.Payments
                            .Where(p => !p.IsDeleted && p.Status == Data.Entities.Sales.PaymentStatus.Completed)
                            .ToList();

                        var cashAmount = paidItems
                            .Where(p => p.PaymentMethod?.Code.Contains("CASH", StringComparison.OrdinalIgnoreCase) == true)
                            .Sum(p => p.Amount);
                        var cardAmount = paidItems
                            .Where(p => p.PaymentMethod?.Code.Contains("CARD", StringComparison.OrdinalIgnoreCase) == true)
                            .Sum(p => p.Amount);
                        var otherAmount = paidItems
                            .Where(p => p.PaymentMethod?.Code.Contains("CASH", StringComparison.OrdinalIgnoreCase) != true
                                     && p.PaymentMethod?.Code.Contains("CARD", StringComparison.OrdinalIgnoreCase) != true)
                            .Sum(p => p.Amount);

                        await fiscalDrawerService.RecordSaleTransactionAsync(
                            drawer.Id, cashAmount, cardAmount, otherAmount, session.Id, currentUser, cancellationToken);
                    }
                }
            }
            catch (Exception fiscalEx)
            {
                logger.LogWarning(fiscalEx, "Failed to record fiscal drawer transaction for session {SessionId}, but session was closed successfully", sessionId);
            }

            // Log audit entry (best effort - don't fail session close if audit fails)
            try
            {
                await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Close", "Open", "Closed", currentUser, "Sale Session", cancellationToken);
            }
            catch (Exception auditEx)
            {
                logger.LogWarning(auditEx, "Failed to log audit entry for session {SessionId}, but session was closed successfully", sessionId);
            }

            logger.LogInformation("Closed sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<SaleSessionDto>> GetPOSSessionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var baseQuery = context.SaleSessions
                .AsNoTracking()
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Use AsSplitQuery to prevent cartesian explosion with multiple collections
            var sessions = await baseQuery
                .AsSplitQuery()
                .Include(s => s.Items.Where(i => !i.IsDeleted))
                .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .OrderByDescending(s => s.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
            var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
            var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

            var operators = await context.StoreUsers
                .AsNoTracking()
                .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            var poses = await context.StorePoses
                .AsNoTracking()
                .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            // Load all products for all sessions in a single batch
            var allProductIds = sessions
                .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var allProducts = await context.Products
                .AsNoTracking()
                .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .Include(p => p.ImageDocument)
                .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                var dto = MapToDtoWithProducts(session, allProducts);
                dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
                dto.PosName = poses.GetValueOrDefault(session.PosId);
                dtos.Add(dto);
            }

            return new PagedResult<SaleSessionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<SaleSessionDto>> GetSessionsByOperatorAsync(Guid operatorId, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var baseQuery = context.SaleSessions
                .AsNoTracking()
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted && s.OperatorId == operatorId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Use AsSplitQuery to prevent cartesian explosion with multiple collections
            var sessions = await baseQuery
                .AsSplitQuery()
                .Include(s => s.Items.Where(i => !i.IsDeleted))
                .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .OrderByDescending(s => s.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
            var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

            var operatorName = await context.StoreUsers
                .AsNoTracking()
                .Where(u => u.Id == operatorId && u.TenantId == currentTenantId.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var poses = await context.StorePoses
                .AsNoTracking()
                .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            // Load all products for all sessions in a single batch
            var allProductIds = sessions
                .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var allProducts = await context.Products
                .AsNoTracking()
                .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .Include(p => p.ImageDocument)
                .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                var dto = MapToDtoWithProducts(session, allProducts);
                dto.OperatorName = operatorName;
                dto.PosName = poses.GetValueOrDefault(session.PosId);
                dtos.Add(dto);
            }

            return new PagedResult<SaleSessionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<SaleSessionDto>> GetSessionsByDateAsync(DateTime startDate, DateTime? endDate, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var end = endDate ?? DateTime.UtcNow;

            var baseQuery = context.SaleSessions
                .AsNoTracking()
                .Where(s => s.TenantId == currentTenantId.Value
                    && !s.IsDeleted
                    && s.CreatedAt >= startDate
                    && s.CreatedAt <= end);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Use AsSplitQuery to prevent cartesian explosion with multiple collections
            var sessions = await baseQuery
                .AsSplitQuery()
                .Include(s => s.Items.Where(i => !i.IsDeleted))
                .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .OrderByDescending(s => s.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
            var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
            var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

            var operators = await context.StoreUsers
                .AsNoTracking()
                .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            var poses = await context.StorePoses
                .AsNoTracking()
                .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            // Load all products for all sessions in a single batch
            var allProductIds = sessions
                .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var allProducts = await context.Products
                .AsNoTracking()
                .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .Include(p => p.ImageDocument)
                .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                var dto = MapToDtoWithProducts(session, allProducts);
                dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
                dto.PosName = poses.GetValueOrDefault(session.PosId);
                dtos.Add(dto);
            }

            return new PagedResult<SaleSessionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<SaleSessionDto>> GetOpenSessionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var baseQuery = context.SaleSessions
                .AsNoTracking()
                .Where(s => s.TenantId == currentTenantId.Value
                    && !s.IsDeleted
                    && !s.ClosedAt.HasValue); // Session still open

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Use AsSplitQuery to prevent cartesian explosion with multiple collections
            var sessions = await baseQuery
                .AsSplitQuery()
                .Include(s => s.Items.Where(i => !i.IsDeleted))
                .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .OrderByDescending(s => s.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
            var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
            var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

            var operators = await context.StoreUsers
                .AsNoTracking()
                .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            var poses = await context.StorePoses
                .AsNoTracking()
                .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            // Load all products for all sessions in a single batch
            var allProductIds = sessions
                .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var allProducts = await context.Products
                .AsNoTracking()
                .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Brand)
                .Include(p => p.VatRate)
                .Include(p => p.ImageDocument)
                .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                var dto = MapToDtoWithProducts(session, allProducts);
                dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
                dto.PosName = poses.GetValueOrDefault(session.PosId);
                dtos.Add(dto);
            }

            return new PagedResult<SaleSessionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<SaleSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var sessions = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .Where(s => s.TenantId == currentTenantId.Value &&
                           !s.IsDeleted &&
                           (s.Status == SaleSessionStatus.Open || s.Status == SaleSessionStatus.Suspended))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                dtos.Add(await MapToDtoAsync(session, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<SaleSessionDto>> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var sessions = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted && s.OperatorId == operatorId && s.Status == SaleSessionStatus.Open)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                dtos.Add(await MapToDtoAsync(session, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Calculates session totals inline without calling SaveChanges.
    /// Used by Add/Update/Remove methods to avoid DbUpdateConcurrencyException.
    /// </summary>
    private void CalculateTotalsInline(SaleSession session)
    {
        CalculateTotals(session);
    }

    private async Task RecalculateTotalsAsync(SaleSession session, CancellationToken cancellationToken)
    {
        CalculateTotals(session);
        session.ModifiedAt = DateTime.UtcNow;
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Shared calculation logic for session totals.
    /// </summary>
    private void CalculateTotals(SaleSession session)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();

        session.OriginalTotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
        var itemsTotal = activeItems.Sum(i => i.TotalAmount);
        session.DiscountAmount = session.OriginalTotal - itemsTotal;
        session.TaxAmount = activeItems.Sum(i => i.TaxAmount);
        session.FinalTotal = itemsTotal + session.TaxAmount;
    }

    private async Task<SaleSessionDto> MapToDtoAsync(SaleSession session, CancellationToken cancellationToken)
    {
        // Get product IDs from items
        var productIds = session.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId).Distinct().ToList();

        // Fetch product details including Brand, VatRate, ImageDocument for all items at once
        var products = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        // Get child session count
        var childSessionCount = await context.SaleSessions
            .AsNoTracking()
            .CountAsync(s => s.ParentSessionId == session.Id && !s.IsDeleted, cancellationToken);

        return MapToDtoWithProducts(session, products, childSessionCount);
    }

    private SaleSessionDto MapToDtoWithProducts(SaleSession session, Dictionary<Guid, EventForge.Server.Data.Entities.Products.Product> products, int childSessionCount = 0)
    {
        var dto = new SaleSessionDto
        {
            Id = session.Id,
            OperatorId = session.OperatorId,
            PosId = session.PosId,
            CustomerId = session.CustomerId,
            SaleType = session.SaleType,
            Status = (SaleSessionStatusDto)session.Status,
            OriginalTotal = session.OriginalTotal,
            DiscountAmount = session.DiscountAmount,
            FinalTotal = session.FinalTotal,
            TaxAmount = session.TaxAmount,
            Currency = session.Currency,
            TableId = session.TableId,
            DocumentId = session.DocumentId,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.ModifiedAt ?? session.CreatedAt,
            ClosedAt = session.ClosedAt,
            CouponCodes = session.CouponCodes,
            ParentSessionId = session.ParentSessionId,
            SplitType = session.SplitType,
            SplitPercentage = session.SplitPercentage,
            MergeReason = session.MergeReason,
            ChildSessionCount = childSessionCount,
            Items = session.Items.Where(i => !i.IsDeleted).Select(i => MapItemToDto(i, products)).ToList(),
            Payments = session.Payments.Where(p => !p.IsDeleted).Select(MapPaymentToDto).ToList(),
            Notes = session.Notes.Select(MapNoteToDto).ToList()
        };

        return dto;
    }

    private SaleItemDto MapItemToDto(SaleItem item, Dictionary<Guid, EventForge.Server.Data.Entities.Products.Product> products)
    {
        var dto = new SaleItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            DiscountPercent = item.DiscountPercent,
            TotalAmount = item.TotalAmount,
            TaxRate = item.TaxRate,
            TaxAmount = item.TaxAmount,
            Notes = item.Notes,
            IsService = item.IsService,
            PromotionId = item.PromotionId
        };

        // Enrich with product details if available
        if (products.TryGetValue(item.ProductId, out var product))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            dto.ProductImageUrl = product.ImageUrl;
#pragma warning restore CS0618 // Type or member is obsolete
            // Use ImageDocument if available, fallback to deprecated ImageUrl
            if (product.ImageDocument is not null)
            {
                dto.ProductThumbnailUrl = product.ImageDocument.ThumbnailStorageKey ?? product.ImageDocument.StorageKey ?? string.Empty;
                dto.ProductImageUrl = product.ImageDocument.Url ?? product.ImageDocument.StorageKey ?? string.Empty;
            }
            dto.BrandName = product.Brand?.Name;
            dto.VatRateId = product.VatRateId;
            dto.VatRateName = product.VatRate?.Name;
            // Note: UnitOfMeasureName would require additional context from ProductCode/ProductUnit
            // For now, we'll leave it null as it requires the specific code that was scanned
        }

        return dto;
    }

    private SalePaymentDto MapPaymentToDto(SalePayment payment)
    {
        return new SalePaymentDto
        {
            Id = payment.Id,
            PaymentMethodId = payment.PaymentMethodId,
            Amount = payment.Amount,
            Status = (PaymentStatusDto)payment.Status,
            TransactionReference = payment.TransactionReference,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }

    private SessionNoteDto MapNoteToDto(SessionNote note)
    {
        return new SessionNoteDto
        {
            Id = note.Id,
            NoteFlagId = note.NoteFlagId,
            NoteFlagName = note.NoteFlag?.Name,
            NoteFlagColor = note.NoteFlag?.Color,
            NoteFlagIcon = note.NoteFlag?.Icon,
            Text = note.Text ?? string.Empty,
            CreatedByUserName = note.CreatedBy,
            CreatedAt = note.CreatedAt
        };
    }

    public async Task<SaleSessionDto?> VoidSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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

            // Only closed sessions can be voided
            if (session.Status != SaleSessionStatus.Closed)
            {
                throw new InvalidOperationException("Only closed sessions can be voided.");
            }

            // Update session status to Cancelled
            session.Status = SaleSessionStatus.Cancelled;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Void", "Closed", "Cancelled", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Voided sale session {SessionId}", sessionId);

            // Create inverse stock movements to restore inventory
            if (session.DocumentId.HasValue)
            {
                foreach (var item in session.Items.Where(i => !i.IsDeleted && !i.IsService))
                {
                    try
                    {
                        var voidMovementDto = new EventForge.DTOs.Warehouse.CreateStockMovementDto
                        {
                            MovementType = "VOID",
                            ProductId = item.ProductId,
                            Quantity = item.Quantity, // Positive to restore inventory
                            MovementDate = DateTime.UtcNow,
                            DocumentHeaderId = session.DocumentId.Value,
                            Reason = "Annullamento vendita",
                            Notes = $"Storno vendita da sessione {session.Id}",
                            Reference = $"VOID-{session.Id.ToString("N")[..8]}"
                        };

                        await stockMovementService.CreateMovementAsync(voidMovementDto, currentUser, cancellationToken);
                        logger.LogInformation("Created void stock movement for product {ProductId}, quantity {Quantity}",
                            item.ProductId, item.Quantity);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating void stock movement for product {ProductId} in session {SessionId}",
                            item.ProductId, session.Id);
                        // Continue with other items even if one fails
                    }
                }

                // Mark document as cancelled
                try
                {
                    var document = await context.DocumentHeaders
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            d => d.Id == session.DocumentId.Value && d.TenantId == currentTenantId.Value && !d.IsDeleted,
                            cancellationToken);

                    if (document is not null)
                    {
                        // Re-attach to modify (only if not already tracked)
                        var entry = context.Entry(document);
                        if (entry.State == EntityState.Detached)
                        {
                            context.Attach(document);
                        }
                        document.Status = EventForge.DTOs.Common.DocumentStatus.Cancelled;
                        document.ModifiedBy = currentUser;
                        document.ModifiedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Marked document {DocumentId} as cancelled", session.DocumentId.Value);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error marking document {DocumentId} as cancelled", session.DocumentId.Value);
                }
            }

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets user ID from username.
    /// </summary>
    private async Task<Guid> GetUserIdFromUsernameAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return Guid.Empty;
            }

            var userId = await context.Users
                .Where(u => u.Username == username && u.TenantId == currentTenantId.Value && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return userId != Guid.Empty ? userId : Guid.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting user ID for username {Username}, using Empty GUID", username);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Generates a receipt document for a closed sale session.
    /// </summary>
    private async Task<Guid?> GenerateReceiptDocumentAsync(SaleSession session, string currentUser, CancellationToken cancellationToken)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                logger.LogWarning("Cannot generate receipt: No tenant context");
                return null;
            }


            // Get or create RECEIPT document type
            DocumentTypeDto receiptDocumentType;
            try
            {
                receiptDocumentType = await documentHeaderService.GetOrCreateReceiptDocumentTypeAsync(currentTenantId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                throw;
            }

            // Get or create System Internal business party if no customer is specified
            Guid businessPartyId;
            if (!session.CustomerId.HasValue)
            {
                try
                {
                    businessPartyId = await documentHeaderService.GetOrCreateSystemBusinessPartyAsync(currentTenantId.Value, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
                businessPartyId = session.CustomerId.Value;
            }

            // Validate items have required data
            var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();
            if (activeItems.Count == 0)
            {
                logger.LogWarning("No active items in session {SessionId}, skipping document generation", session.Id);
                return null;
            }


            // Create document header
            var createDocumentDto = new EventForge.DTOs.Documents.CreateDocumentHeaderDto
            {
                DocumentTypeId = receiptDocumentType.Id,
                Number = null, // Will be auto-generated
                Date = DateTime.UtcNow,
                BusinessPartyId = businessPartyId,
                CashRegisterId = session.PosId,
                CashierId = session.OperatorId,
                Currency = session.Currency ?? "EUR",
                IsFiscal = true,
                TotalDiscountAmount = session.DiscountAmount,
                Notes = $"Generato dalla sessione di vendita {session.Id}",
                Rows = activeItems.Select(item => new EventForge.DTOs.Documents.CreateDocumentRowDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Description = item.ProductName
                }).ToList()
            };

            DocumentHeaderDto? documentHeader;
            try
            {
                documentHeader = await documentHeaderService.CreateDocumentHeaderAsync(createDocumentDto, currentUser, cancellationToken);
                if (documentHeader is null)
                {
                    logger.LogError("CreateDocumentHeaderAsync returned null for session {SessionId}", session.Id);
                    return null;
                }
                logger.LogInformation("Document header created: {DocumentId} for session {SessionId}", documentHeader.Id, session.Id);
            }
            catch (Exception ex)
            {
                throw;
            }

            // Create stock movements for each item (outbound)
            var stockMovementErrors = 0;
            foreach (var item in activeItems.Where(i => !i.IsService))
            {
                try
                {

                    var movementDto = new EventForge.DTOs.Warehouse.CreateStockMovementDto
                    {
                        MovementType = "Outbound",
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity, // Negative for outbound
                        MovementDate = DateTime.UtcNow,
                        DocumentHeaderId = documentHeader.Id,
                        Reason = "Sale",
                        Notes = $"Vendita da sessione {session.Id}",
                        Reference = $"SESS-{session.Id.ToString("N")[..8]}"
                    };

                    await stockMovementService.CreateMovementAsync(movementDto, currentUser, cancellationToken);
                    logger.LogInformation("Created stock movement for product {ProductId}, quantity {Quantity} for document {DocumentId}",
                        item.ProductId, -item.Quantity, documentHeader.Id);
                }
                catch (Exception ex)
                {
                    stockMovementErrors++;
                    logger.LogError(ex, "Error creating stock movement for product {ProductId} in session {SessionId}. Continuing with other items.",
                        item.ProductId, session.Id);
                    // Continue with other items even if one fails
                }
            }

            if (stockMovementErrors > 0)
            {
                logger.LogWarning("Completed document {DocumentId} creation with {ErrorCount} stock movement errors for session {SessionId}",
                    documentHeader.Id, stockMovementErrors, session.Id);
            }
            else
            {
                logger.LogInformation("Document {DocumentId} created successfully with all stock movements for session {SessionId}",
                    documentHeader.Id, session.Id);
            }

            return documentHeader.Id;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Logs detailed entity states for diagnostic purposes. 
    /// This method should only be called in error/catch blocks to avoid excessive logging.
    /// Note: This method iterates through ChangeTracker entries which is acceptable since
    /// it's only called during error scenarios and diagnostic accuracy is prioritized.
    /// </summary>
    private void LogDetailedEntityStates(Guid sessionId, Guid tenantId)
    {
        try
        {
            // Calculate items count from ChangeTracker
            // Note: LINQ iteration is acceptable here as this is only called on errors
            var itemsCount = context.ChangeTracker.Entries()
                .Count(e => e.Entity is SaleItem item && item.SaleSessionId == sessionId);

            logger.LogError(
                "Diagnostic - SessionId: {SessionId}, TenantId: {TenantId}, ItemsCount: {ItemCount}, TrackedEntities: {TrackedCount}",
                sessionId,
                tenantId,
                itemsCount,
                context.ChangeTracker.Entries().Count());

            // Log all tracked entities and their states
            foreach (var entry in context.ChangeTracker.Entries())
            {
                var entityType = entry.Entity.GetType().Name;
                var entityId = entry.Entity is AuditableEntity ae ? ae.Id.ToString() : "N/A";
                var entityState = entry.State.ToString();

                logger.LogError(
                    "Tracked entity: Type={EntityType}, Id={EntityId}, State={State}",
                    entityType,
                    entityId,
                    entityState);

                // Log IsDeleted for AuditableEntity
                if (entry.Entity is AuditableEntity auditableEntity)
                {
                    logger.LogError(
                        "  -> IsDeleted={IsDeleted}, TenantId={TenantId}",
                        auditableEntity.IsDeleted,
                        auditableEntity.TenantId);
                }

                // Log current and original values for modified entities
                if (entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties.Where(p => p.IsModified))
                    {
                        logger.LogError(
                            "  -> Modified property: {PropertyName}, OriginalValue={OriginalValue}, CurrentValue={CurrentValue}",
                            property.Metadata.Name,
                            property.OriginalValue,
                            property.CurrentValue);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging detailed entity states");
        }
    }

    public async Task<SplitResultDto?> SplitSessionAsync(SplitSessionDto splitDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            // Validate and retrieve session
            var session = await context.SaleSessions
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == splitDto.SessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
                return null;

            // Validate session can be split
            if (session.Status != SaleSessionStatus.Open)
                throw new InvalidOperationException("Solo le sessioni aperte possono essere splittate.");

            if (session.ParentSessionId is not null)
                throw new InvalidOperationException("Una sessione già splitta non può essere nuovamente splitta.");

            if (!session.Items.Any(i => !i.IsDeleted))
                throw new InvalidOperationException("La sessione deve avere almeno un item per essere splitta.");

            // Validate split-specific parameters
            ValidateSplitParameters(splitDto, session);

            // Create child sessions
            var childSessions = new List<SaleSession>();
            var splitType = splitDto.SplitType.ToString().ToUpperInvariant();

            for (var i = 0; i < splitDto.NumberOfPeople; i++)
            {
                var childSession = new SaleSession
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId.Value,
                    OperatorId = session.OperatorId,
                    PosId = session.PosId,
                    CustomerId = session.CustomerId,
                    SaleType = session.SaleType,
                    TableId = session.TableId,
                    Currency = session.Currency,
                    Status = SaleSessionStatus.Open,
                    ParentSessionId = session.Id,
                    SplitType = splitType,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = currentUser,
                    ModifiedAt = DateTime.UtcNow
                };

                // Add items based on split type
                switch (splitDto.SplitType)
                {
                    case SplitTypeDto.Equal:
                        AddItemsForEqualSplit(session, childSession, i, splitDto.NumberOfPeople, currentUser);
                        break;
                    case SplitTypeDto.ByItems:
                        AddItemsForItemsSplit(session, childSession, i, splitDto.ItemAssignments!, currentUser);
                        break;
                    case SplitTypeDto.Percentage:
                        AddItemsForPercentageSplit(session, childSession, splitDto.Percentages![i], currentUser);
                        childSession.SplitPercentage = splitDto.Percentages![i];
                        break;
                }

                CalculateSessionTotals(childSession);
                childSessions.Add(childSession);
                _ = context.SaleSessions.Add(childSession);
            }

            // Update parent session status
            session.Status = SaleSessionStatus.Splitting;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Split", "Open", "Splitting", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Split session {SessionId} into {Count} child sessions", session.Id, childSessions.Count);

            // Map to DTOs
            var childDtos = new List<SaleSessionDto>();
            foreach (var child in childSessions)
            {
                childDtos.Add(await MapToDtoAsync(child, cancellationToken));
            }

            return new SplitResultDto
            {
                OriginalSessionId = session.Id,
                ChildSessions = childDtos,
                TotalAmount = session.FinalTotal,
                SplitType = splitDto.SplitType
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SaleSessionDto?> MergeSessionsAsync(MergeSessionsDto mergeDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            // Validate and retrieve sessions
            var sessions = await context.SaleSessions
                .Include(s => s.Items)
                .Where(s => mergeDto.SessionIds.Contains(s.Id) && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            if (sessions.Count != mergeDto.SessionIds.Count)
                return null;

            // Validate sessions can be merged
            if (sessions.Any(s => s.Status != SaleSessionStatus.Open))
                throw new InvalidOperationException("Solo le sessioni aperte possono essere merge.");

            if (sessions.Any(s => s.ParentSessionId != null))
                throw new InvalidOperationException("Sessioni già splittate non possono essere merge.");

            if (sessions.Select(s => s.TenantId).Distinct().Count() > 1)
                throw new InvalidOperationException("Tutte le sessioni devono appartenere allo stesso tenant.");

            // Create merged session
            var firstSession = sessions.First();
            var mergedSession = new SaleSession
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                OperatorId = firstSession.OperatorId,
                PosId = firstSession.PosId,
                CustomerId = firstSession.CustomerId,
                SaleType = firstSession.SaleType,
                TableId = mergeDto.TargetTableId ?? firstSession.TableId,
                Currency = firstSession.Currency,
                Status = SaleSessionStatus.Open,
                MergeReason = mergeDto.MergeReason,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            // Copy all items from all sessions
            foreach (var session in sessions)
            {
                foreach (var item in session.Items.Where(i => !i.IsDeleted))
                {
                    var newItem = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId.Value,
                        SaleSessionId = mergedSession.Id,
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        DiscountPercent = item.DiscountPercent,
                        TotalAmount = item.TotalAmount,
                        TaxRate = item.TaxRate,
                        TaxAmount = item.TaxAmount,
                        Notes = item.Notes,
                        IsService = item.IsService,
                        PromotionId = item.PromotionId,
                        CreatedBy = currentUser,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedBy = currentUser,
                        ModifiedAt = DateTime.UtcNow
                    };
                    mergedSession.Items.Add(newItem);
                }
            }

            // Calculate totals
            CalculateSessionTotals(mergedSession);

            // Add merged session
            _ = context.SaleSessions.Add(mergedSession);

            // Cancel original sessions
            foreach (var session in sessions)
            {
                session.Status = SaleSessionStatus.Cancelled;
                session.ModifiedBy = currentUser;
                session.ModifiedAt = DateTime.UtcNow;
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await auditLogService.LogEntityChangeAsync("SaleSession", mergedSession.Id, "Status", "Merge", null, "Open", currentUser, "Sale Session", cancellationToken);

            logger.LogInformation("Merged {Count} sessions into new session {SessionId}", sessions.Count, mergedSession.Id);

            return await MapToDtoAsync(mergedSession, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<SaleSessionDto>> GetChildSessionsAsync(Guid parentSessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var childSessions = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .Where(s => s.ParentSessionId == parentSessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            var result = new List<SaleSessionDto>();
            foreach (var session in childSessions)
            {
                result.Add(await MapToDtoAsync(session, cancellationToken));
            }

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> CanSplitSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return false;
            }

            var session = await context.SaleSessions
                .AsNoTracking()
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session is null)
                return false;

            return session.Status == SaleSessionStatus.Open &&
                   session.ParentSessionId is null &&
                   session.Items.Any(i => !i.IsDeleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if session {SessionId} can be split", sessionId);
            return false;
        }
    }

    public async Task<bool> CanMergeSessionsAsync(List<Guid> sessionIds, CancellationToken cancellationToken = default)
    {
        try
        {
            if (sessionIds.Count < 2)
                return false;

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return false;
            }

            var sessions = await context.SaleSessions
                .AsNoTracking()
                .Where(s => sessionIds.Contains(s.Id) && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            if (sessions.Count != sessionIds.Count)
                return false;

            return sessions.All(s => s.Status == SaleSessionStatus.Open && s.ParentSessionId == null) &&
                   sessions.Select(s => s.TenantId).Distinct().Count() == 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if sessions can be merged");
            return false;
        }
    }

    private void ValidateSplitParameters(SplitSessionDto splitDto, SaleSession session)
    {
        switch (splitDto.SplitType)
        {
            case SplitTypeDto.ByItems:
                if (splitDto.ItemAssignments is null || !splitDto.ItemAssignments.Any())
                    throw new InvalidOperationException("Gli assegnamenti degli item sono richiesti per lo split BY_ITEMS.");

                var itemIds = session.Items.Where(i => !i.IsDeleted).Select(i => i.Id).ToHashSet();
                var assignedItems = splitDto.ItemAssignments.Select(a => a.ItemId).ToHashSet();

                if (!itemIds.SetEquals(assignedItems))
                    throw new InvalidOperationException("Tutti gli items devono essere assegnati esattamente una volta.");

                if (splitDto.ItemAssignments.Any(a => a.PersonIndex < 0 || a.PersonIndex >= splitDto.NumberOfPeople))
                    throw new InvalidOperationException($"PersonIndex deve essere tra 0 e {splitDto.NumberOfPeople - 1}.");
                break;

            case SplitTypeDto.Percentage:
                if (splitDto.Percentages is null || splitDto.Percentages.Count != splitDto.NumberOfPeople)
                    throw new InvalidOperationException("Le percentuali devono essere fornite per ogni persona.");

                var sum = splitDto.Percentages.Sum();
                if (Math.Abs(sum - 100) > PercentageSumTolerance)
                    throw new InvalidOperationException($"Le percentuali devono sommare a 100. Somma attuale: {sum}");
                break;
        }
    }

    private void AddItemsForEqualSplit(SaleSession parentSession, SaleSession childSession, int childIndex, int totalChildren, string currentUser)
    {
        var activeItems = parentSession.Items.Where(i => !i.IsDeleted).ToList();
        var itemsPerChild = activeItems.Count / totalChildren;
        var remainder = activeItems.Count % totalChildren;

        // Determine which items belong to this child
        var startIndex = childIndex * itemsPerChild + Math.Min(childIndex, remainder);
        var count = itemsPerChild + (childIndex < remainder ? 1 : 0);

        var childItems = activeItems.Skip(startIndex).Take(count).ToList();

        foreach (var item in childItems)
        {
            var newItem = CreateCopiedItem(item, childSession.Id, currentUser);
            childSession.Items.Add(newItem);
        }
    }

    private void AddItemsForItemsSplit(SaleSession parentSession, SaleSession childSession, int personIndex, List<SplitItemAssignmentDto> assignments, string currentUser)
    {
        var itemsForPerson = assignments.Where(a => a.PersonIndex == personIndex).Select(a => a.ItemId).ToHashSet();
        var parentItems = parentSession.Items.Where(i => !i.IsDeleted && itemsForPerson.Contains(i.Id)).ToList();

        foreach (var item in parentItems)
        {
            var newItem = CreateCopiedItem(item, childSession.Id, currentUser);
            childSession.Items.Add(newItem);
        }
    }

    private void AddItemsForPercentageSplit(SaleSession parentSession, SaleSession childSession, decimal percentage, string currentUser)
    {
        var activeItems = parentSession.Items.Where(i => !i.IsDeleted).ToList();

        foreach (var item in activeItems)
        {
            var newItem = CreateCopiedItem(item, childSession.Id, currentUser);
            // Adjust quantity based on percentage
            newItem.Quantity = item.Quantity * (percentage / 100m);
            newItem.TotalAmount = item.TotalAmount * (percentage / 100m);
            newItem.TaxAmount = item.TaxAmount * (percentage / 100m);
            childSession.Items.Add(newItem);
        }
    }

    private SaleItem CreateCopiedItem(SaleItem source, Guid newSessionId, string currentUser)
    {
        return new SaleItem
        {
            Id = Guid.NewGuid(),
            TenantId = source.TenantId,
            SaleSessionId = newSessionId,
            ProductId = source.ProductId,
            ProductCode = source.ProductCode,
            ProductName = source.ProductName,
            UnitPrice = source.UnitPrice,
            Quantity = source.Quantity,
            DiscountPercent = source.DiscountPercent,
            TotalAmount = source.TotalAmount,
            TaxRate = source.TaxRate,
            TaxAmount = source.TaxAmount,
            Notes = source.Notes,
            IsService = source.IsService,
            PromotionId = source.PromotionId,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };
    }

    private void CalculateSessionTotals(SaleSession session)
    {
        var items = session.Items.Where(i => !i.IsDeleted).ToList();
        session.OriginalTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        session.TaxAmount = items.Sum(i => i.TaxAmount);
        session.FinalTotal = items.Sum(i => i.TotalAmount);
        session.DiscountAmount = session.OriginalTotal - session.FinalTotal;
    }

    /// <summary>
    /// Applies active promotion rules to all non-deleted items of the session.
    /// Updates <see cref="SaleItem.DiscountPercent"/> and <see cref="SaleItem.PromotionId"/>
    /// for items that receive a promotional discount, then recalculates session totals.
    /// Any failure (e.g. promotion service unavailable) is logged and silently ignored
    /// so it never prevents the item from being saved.
    /// </summary>
    private async Task ApplyPromotionsToSessionItemsAsync(
        SaleSession session,
        string currentUser,
        CancellationToken cancellationToken)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();
        if (activeItems.Count == 0)
            return;

        try
        {
            // Parse coupon codes stored as comma-separated string
            var couponCodes = string.IsNullOrWhiteSpace(session.CouponCodes)
                ? null
                : session.CouponCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // Fetch product category info for all products in one query
            var productIds = activeItems.Select(i => i.ProductId).Distinct().ToList();
            var productCategories = await context.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, p.CategoryNodeId })
                .ToDictionaryAsync(p => p.Id, p => p.CategoryNodeId, cancellationToken);

            var applyDto = new ApplyPromotionRulesDto
            {
                CartItems = activeItems.Select(item => new CartItemDto
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = (int)Math.Ceiling(item.Quantity),
                    CategoryIds = productCategories.TryGetValue(item.ProductId, out var catId) && catId.HasValue
                        ? new List<Guid> { catId.Value }
                        : null,
                    ExistingLineDiscount = item.DiscountPercent
                }).ToList(),
                CustomerId = session.CustomerId,
                CouponCodes = couponCodes,
                OrderDateTime = DateTime.UtcNow,
                Currency = session.Currency ?? "EUR"
            };

            var result = await promotionService.ApplyPromotionRulesAsync(applyDto, cancellationToken);

            if (!result.Success || result.CartItems.Count == 0)
                return;

            bool anyChanged = false;

            for (int i = 0; i < activeItems.Count && i < result.CartItems.Count; i++)
            {
                var saleItem = activeItems[i];
                var promoItem = result.CartItems[i];

                if (promoItem.AppliedPromotions.Count == 0)
                    continue;

                // Use the effective discount from the promotion engine if it's greater than existing
                var promoDiscount = promoItem.EffectiveDiscountPercentage;
                if (promoDiscount > saleItem.DiscountPercent)
                {
                    saleItem.DiscountPercent = promoDiscount;
                    saleItem.PromotionId = promoItem.AppliedPromotions[0].PromotionId;

                    var subtotal = saleItem.UnitPrice * saleItem.Quantity;
                    var discountAmount = subtotal * (saleItem.DiscountPercent / 100m);
                    saleItem.TotalAmount = subtotal - discountAmount;
                    saleItem.TaxAmount = saleItem.TotalAmount * (saleItem.TaxRate / 100m);
                    saleItem.ModifiedAt = DateTime.UtcNow;
                    saleItem.ModifiedBy = currentUser;
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                CalculateSessionTotals(session);
                session.ModifiedAt = DateTime.UtcNow;
                session.ModifiedBy = currentUser;
                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Promotions applied to session {SessionId}: {PromotionCount} promotion(s), TotalDiscount={TotalDiscount:C2}",
                    session.Id, result.AppliedPromotions.Count, result.TotalDiscountAmount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to apply promotions to session {SessionId}; items saved without promotion discount",
                session.Id);
        }
    }

    public async Task<IEnumerable<Guid>> GetCustomerPurchasedProductIdsAsync(Guid customerId, int maxSessions = 30, CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

}
