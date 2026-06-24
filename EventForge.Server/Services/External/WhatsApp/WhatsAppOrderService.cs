using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.AI;
using System.Text.Json;

namespace EventForge.Server.Services.External.WhatsApp;

/// <summary>
/// Creates documents from completed AI order sessions and manages session lifecycle.
/// </summary>
public class WhatsAppOrderService(
    EventForgeDbContext dbContext,
    ILogger<WhatsAppOrderService> logger) : IWhatsAppOrderService
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<OrderConversationSession> GetOrCreateSessionAsync(
        Guid chatThreadId,
        Guid? businessPartyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.OrderConversationSessions
            .FirstOrDefaultAsync(s => s.ChatThreadId == chatThreadId && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        if (existing != null) return existing;

        var session = new OrderConversationSession
        {
            TenantId = tenantId,
            ChatThreadId = chatThreadId,
            BusinessPartyId = businessPartyId,
            State = OrderConversationState.Idle,
            CreatedBy = "AI"
        };
        dbContext.OrderConversationSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateSessionAsync(
        OrderConversationSession session,
        OrderConversationState newState,
        string? draftJson,
        CancellationToken cancellationToken = default)
    {
        session.State = newState;
        session.DraftJson = draftJson;
        session.LastAiPromptAt = DateTime.UtcNow;
        session.ModifiedAt = DateTime.UtcNow;
        session.ModifiedBy = "AI";
        session.AiRoundCount++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateDocumentFromSessionAsync(
        OrderConversationSession session,
        Guid tenantId,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(session.DraftJson))
            throw new InvalidOperationException("Session has no draft to create a document from.");

        List<OrderDraftItem> draftItems;
        try
        {
            draftItems = JsonSerializer.Deserialize<List<OrderDraftItem>>(session.DraftJson, _jsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialise draft JSON for session {SessionId}", session.Id);
            throw new InvalidOperationException("Invalid draft JSON in session.", ex);
        }

        if (draftItems.Count == 0)
            throw new InvalidOperationException("Draft contains no items.");

        // Resolve a document type for orders (first active type found, admin should configure this)
        var documentType = await dbContext.DocumentTypes
            .AsNoTracking()
            .Where(dt => dt.TenantId == tenantId && !dt.IsDeleted && dt.IsActive)
            .OrderBy(dt => dt.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentType == null)
            throw new InvalidOperationException("No DocumentType configured for tenant. Please create at least one document type.");

        if (session.BusinessPartyId == null)
            throw new InvalidOperationException("Cannot create a document from a session with no associated BusinessParty.");

        var header = new DocumentHeader
        {
            TenantId = tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = session.BusinessPartyId.Value,
            Date = DateTime.UtcNow,
            Number = $"WA-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            DocumentReason = "Ordine ricevuto via WhatsApp (AI)",
            Notes = $"Creato automaticamente da conversazione WhatsApp. SessioneId: {session.Id}",
            CreatedBy = createdBy,
            IsFiscal = false
        };

        dbContext.DocumentHeaders.Add(header);

        // Resolve products and create rows
        var productIds = draftItems
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var products = productIds.Count > 0
            ? await dbContext.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId)
                .ToDictionaryAsync(p => p.Id, cancellationToken)
            : new Dictionary<Guid, Product>();

        foreach (var item in draftItems.Where(i => !i.IsNotFound))
        {
            Product? product = null;
            if (item.ProductId.HasValue) products.TryGetValue(item.ProductId.Value, out product);

            if (product == null && !string.IsNullOrWhiteSpace(item.ProductCode))
                product = await dbContext.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Code == item.ProductCode && p.TenantId == tenantId && !p.IsDeleted, cancellationToken);

            var row = new DocumentRow
            {
                TenantId = tenantId,
                DocumentHeaderId = header.Id,
                ProductId = product?.Id,
                ProductCode = product?.Code ?? item.ProductCode,
                Description = product?.Name ?? item.ProductName ?? item.RawText,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice ?? product?.DefaultPrice ?? 0m,
                UnitOfMeasure = item.UnitOfMeasure,
                IsManual = item.UnitPrice.HasValue,
                RowType = EventForge.Server.Data.Entities.Documents.DocumentRowType.Product,
                CreatedBy = createdBy
            };
            dbContext.DocumentRows.Add(row);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Update session with the document reference
        session.CreatedDocumentHeaderId = header.Id;
        session.ModifiedAt = DateTime.UtcNow;
        session.ModifiedBy = createdBy;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("WhatsApp order document {DocumentId} created for session {SessionId}", header.Id, session.Id);
        return header.Id;
    }
}
