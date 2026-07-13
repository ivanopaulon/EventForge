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
    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto addPaymentDto, string currentUser, CancellationToken cancellationToken = default)
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

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, string currentUser, CancellationToken cancellationToken = default)
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

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto addNoteDto, string currentUser, CancellationToken cancellationToken = default)
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

}
