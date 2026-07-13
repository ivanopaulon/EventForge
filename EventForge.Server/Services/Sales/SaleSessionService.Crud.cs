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
    public async Task<SaleSessionDto> CreateSessionAsync(CreateSaleSessionDto createDto, string currentUser, CancellationToken cancellationToken = default)
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

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
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

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
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
        session.FidelityCardId = updateDto.FidelityCardId ?? session.FidelityCardId;
        session.SaleType = updateDto.SaleType ?? session.SaleType;

        if (updateDto.ClearTable)
        {
            session.TableId = null;
        }
        else if (updateDto.TableId.HasValue && updateDto.TableId.Value != Guid.Empty)
        {
            session.TableId = updateDto.TableId.Value;
        }

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

    public async Task<bool> DeleteSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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

}
