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
    public async Task<SaleSessionDto?> VoidSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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
                    var voidMovementDto = new Prym.DTOs.Warehouse.CreateStockMovementDto
                    {
                        MovementType = StockMovementType.Return.ToString(),
                        ProductId = item.ProductId,
                        Quantity = item.Quantity, // Positive quantity: restores inventory
                        MovementDate = DateTime.UtcNow,
                        DocumentHeaderId = session.DocumentId.Value,
                        Reason = "Return",
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

            // Mark document as archived
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
                    document.Status = Prym.DTOs.Common.DocumentStatus.Archived;
                    document.ModifiedBy = currentUser;
                    document.ModifiedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Marked document {DocumentId} as archived", session.DocumentId.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking document {DocumentId} as archived", session.DocumentId.Value);
            }
        }

        return await MapToDtoAsync(session, cancellationToken);
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
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            logger.LogWarning("Cannot generate receipt: No tenant context");
            return null;
        }


        // Get or create RECEIPT document type
        DocumentTypeDto receiptDocumentType;
        receiptDocumentType = await documentHeaderService.GetOrCreateReceiptDocumentTypeAsync(currentTenantId.Value, cancellationToken);

        // Get or create System Internal business party if no customer is specified
        Guid businessPartyId;
        if (!session.CustomerId.HasValue)
        {
            businessPartyId = await documentHeaderService.GetOrCreateSystemBusinessPartyAsync(currentTenantId.Value, cancellationToken);
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
        var createDocumentDto = new Prym.DTOs.Documents.CreateDocumentHeaderDto
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
            Rows = activeItems.Select(item => new Prym.DTOs.Documents.CreateDocumentRowDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Description = item.ProductName
            }).ToList()
        };

        DocumentHeaderDto? documentHeader;
        documentHeader = await documentHeaderService.CreateDocumentHeaderAsync(createDocumentDto, currentUser, cancellationToken);
        if (documentHeader is null)
        {
            logger.LogError("CreateDocumentHeaderAsync returned null for session {SessionId}", session.Id);
            return null;
        }
        logger.LogInformation("Document header created: {DocumentId} for session {SessionId}", documentHeader.Id, session.Id);

        // Create stock movements for each item (outbound)
        var stockMovementErrors = 0;
        foreach (var item in activeItems.Where(i => !i.IsService))
        {
            try
            {

                var movementDto = new Prym.DTOs.Warehouse.CreateStockMovementDto
                {
                    MovementType = "Outbound",
                    ProductId = item.ProductId,
                    Quantity = item.Quantity, // Positive quantity: direction is determined by MovementType
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

}
