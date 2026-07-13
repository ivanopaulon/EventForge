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
    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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

}
