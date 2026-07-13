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
    /// <summary>
    /// Calculates session totals inline without calling SaveChanges.
    /// Used by Add/Update/Remove methods to avoid DbUpdateConcurrencyException.
    /// </summary>
    private async Task CalculateTotalsInlineAsync(SaleSession session, CancellationToken cancellationToken)
    {
        await CalculateTotalsAsync(session, cancellationToken);
    }

    private async Task RecalculateTotalsAsync(SaleSession session, CancellationToken cancellationToken)
    {
        await CalculateTotalsAsync(session, cancellationToken);
        session.ModifiedAt = DateTime.UtcNow;
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Shared calculation logic for session totals.
    /// </summary>
    /// <remarks>
    /// FIDELITY DISCOUNT — trattamento IVA confermato dal commercialista del progetto.
    /// Il ricalcolo proporzionale dell'IVA quando si applica lo sconto fidelity aggiuntivo dopo il
    /// calcolo per-riga è corretto per lo sconto fidelity attuale (percentuale uniforme sull'intero
    /// carrello). Vedi motivazione completa nel commento sulla formula sotto e in
    /// docs/decision-log/ADR-FIDELITY-DISCOUNT-VAT.md.
    /// </remarks>
    private async Task CalculateTotalsAsync(SaleSession session, CancellationToken cancellationToken)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();

        session.OriginalTotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
        var itemsTotal = activeItems.Sum(i => i.TotalAmount);

        decimal fidelityDiscountAmount = 0m;
        if (session.FidelityCardId.HasValue)
        {
            var card = await context.FidelityCards.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == session.FidelityCardId.Value && c.Status == Data.Entities.Business.FidelityCardStatus.Active, cancellationToken);
            if (card != null && card.DiscountPercentage > 0)
            {
                fidelityDiscountAmount = itemsTotal * (card.DiscountPercentage / 100m);
            }
        }

        session.DiscountAmount = session.OriginalTotal - itemsTotal + fidelityDiscountAmount;

        // Sconto fidelity applicato proporzionalmente a imponibile e IVA. Base normativa: art. 26
        // DPR 633/1972 — gli sconti in denaro incondizionati riducono direttamente il corrispettivo,
        // e la base imponibile in fattura va calcolata al netto dello sconto concesso (distinto dallo
        // sconto in natura ex art. 15, non applicabile qui). La formula aggregata sotto è equivalente
        // matematicamente a ridurre ogni riga (e la sua aliquota IVA specifica) della stessa
        // percentuale — vale perché lo sconto fidelity è sempre una percentuale uniforme su tutto il
        // carrello. Se in futuro si introduce uno sconto fidelity a importo fisso, questa equivalenza
        // non vale più e la formula va rifatta per operare riga per riga.
        // Confermato dal commercialista del progetto — vedi docs/decision-log/ADR-FIDELITY-DISCOUNT-VAT.md.
        var vatRatio = itemsTotal > 0 ? fidelityDiscountAmount / itemsTotal : 0;
        session.TaxAmount = activeItems.Sum(i => i.TaxAmount) * (1 - vatRatio);

        session.FinalTotal = itemsTotal - fidelityDiscountAmount + session.TaxAmount;
    }

    private async Task<SaleSessionDto> MapToDtoAsync(SaleSession session, CancellationToken cancellationToken, List<PromotionNearMissDto>? nearMissPromotions = null)
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

        return MapToDtoWithProducts(session, products, childSessionCount, nearMissPromotions);
    }

    private SaleSessionDto MapToDtoWithProducts(SaleSession session, Dictionary<Guid, EventForge.Server.Data.Entities.Products.Product> products, int childSessionCount = 0, List<PromotionNearMissDto>? nearMissPromotions = null)
    {
        var dto = new SaleSessionDto
        {
            Id = session.Id,
            OperatorId = session.OperatorId,
            PosId = session.PosId,
            CustomerId = session.CustomerId,
            FidelityCardId = session.FidelityCardId,
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
            Notes = session.Notes.Select(MapNoteToDto).ToList(),
            NearMissPromotions = nearMissPromotions ?? new List<PromotionNearMissDto>()
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
            PromotionId = item.PromotionId,
            PriceListId = item.PriceListId,
            PriceListName = item.PriceListName,
            AppliedPromotionsJSON = item.AppliedPromotionsJSON
        };

        // Enrich with product details if available
        if (products.TryGetValue(item.ProductId, out var product))
        {
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

}
