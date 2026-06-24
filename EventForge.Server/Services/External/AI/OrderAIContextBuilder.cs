using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.AI;
using Prym.DTOs.Products;
using System.Text.Json;

namespace EventForge.Server.Services.External.AI;

/// <summary>
/// Builds per-customer AI context: product catalogue, order history, and system prompt.
/// </summary>
public class OrderAIContextBuilder(
    EventForgeDbContext dbContext,
    IConfiguration configuration,
    ILogger<OrderAIContextBuilder> logger) : IOrderAIContextBuilder
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<OrderDraftContext> BuildContextAsync(
        Guid chatThreadId,
        Guid tenantId,
        string? draftJson,
        string? lastCustomerMessage,
        CancellationToken cancellationToken = default)
    {
        var thread = await dbContext.ChatThreads
            .AsNoTracking()
            .Include(t => t.BusinessParty)
            .FirstOrDefaultAsync(t => t.Id == chatThreadId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        var session = await dbContext.OrderConversationSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ChatThreadId == chatThreadId && s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        var ctx = new OrderDraftContext
        {
            ChatThreadId = chatThreadId,
            BusinessPartyId = thread?.BusinessPartyId,
            BusinessPartyName = thread?.BusinessParty?.Name,
            State = session?.State ?? OrderConversationState.Idle,
            LastCustomerMessage = lastCustomerMessage,
            ConversationLocale = "it"
        };

        if (!string.IsNullOrWhiteSpace(draftJson))
        {
            try
            {
                ctx.Items = JsonSerializer.Deserialize<List<OrderDraftItem>>(draftJson, _jsonOpts) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not deserialise draft JSON for thread {ThreadId}", chatThreadId);
            }
        }
        else if (!string.IsNullOrWhiteSpace(session?.DraftJson))
        {
            try
            {
                ctx.Items = JsonSerializer.Deserialize<List<OrderDraftItem>>(session.DraftJson, _jsonOpts) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not deserialise session draft JSON for thread {ThreadId}", chatThreadId);
            }
        }

        return ctx;
    }

    public async Task<List<ProductDto>> GetCatalogForPartyAsync(
        Guid? businessPartyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var maxItems = int.TryParse(configuration["OpenAI:CatalogMaxItems"], out var v) ? v : 200;

            var query = dbContext.Products
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.Name)
                .Take(maxItems);

            var products = await query.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                DefaultPrice = p.DefaultPrice,
                ShortDescription = p.ShortDescription
            }).ToListAsync(cancellationToken);

            return products;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetCatalogForPartyAsync failed for BusinessParty {BpId}", businessPartyId);
            return [];
        }
    }

    public async Task<string> BuildSystemPromptAsync(
        OrderDraftContext ctx,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Load per-tenant AI settings if they exist
        var settings = await dbContext.AIOrderSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted, cancellationToken);

        var baseTemplate = settings?.SystemPromptTemplate
            ?? "Sei l'assistente ordini WhatsApp di un negozio. Aiuta il cliente in modo professionale e cordiale. Rispondi in italiano.";

        // Load recent order history (last 5 orders)
        var recentOrders = "";
        if (ctx.BusinessPartyId.HasValue)
        {
            try
            {
                var orders = await dbContext.DocumentHeaders
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenantId
                        && d.BusinessPartyId == ctx.BusinessPartyId.Value
                        && !d.IsDeleted)
                    .OrderByDescending(d => d.Date)
                    .Take(5)
                    .Select(d => new { d.Number, d.Date, d.TotalGrossAmount })
                    .ToListAsync(cancellationToken);

                if (orders.Count > 0)
                {
                    recentOrders = "\n\nUltimi ordini del cliente:\n" +
                        string.Join("\n", orders.Select(o => $"- Ordine {o.Number} del {o.Date:dd/MM/yyyy}: €{o.TotalGrossAmount:0.00}"));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load recent orders for BusinessParty {BpId}", ctx.BusinessPartyId);
            }
        }

        return baseTemplate
            .Replace("{BusinessPartyName}", ctx.BusinessPartyName ?? "Cliente")
            + recentOrders;
    }
}
