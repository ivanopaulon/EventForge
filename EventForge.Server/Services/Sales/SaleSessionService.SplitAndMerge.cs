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
    public async Task<SplitResultDto?> SplitSessionAsync(SplitSessionDto splitDto, string currentUser, CancellationToken cancellationToken = default)
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
                FidelityCardId = session.FidelityCardId,
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

    public async Task<SaleSessionDto?> MergeSessionsAsync(MergeSessionsDto mergeDto, string currentUser, CancellationToken cancellationToken = default)
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
            FidelityCardId = firstSession.FidelityCardId,
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
                    PriceListId = item.PriceListId,
                    PriceListName = item.PriceListName,
                    AppliedPromotionsJSON = item.AppliedPromotionsJSON,
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

    public async Task<List<SaleSessionDto>> GetChildSessionsAsync(Guid parentSessionId, CancellationToken cancellationToken = default)
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
            ManualDiscountPercent = source.ManualDiscountPercent,
            PromotionDiscountPercent = source.PromotionDiscountPercent,
            TotalAmount = source.TotalAmount,
            TaxRate = source.TaxRate,
            TaxAmount = source.TaxAmount,
            Notes = source.Notes,
            IsService = source.IsService,
            PromotionId = source.PromotionId,
            PriceListId = source.PriceListId,
            PriceListName = source.PriceListName,
            AppliedPromotionsJSON = source.AppliedPromotionsJSON,
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
    private async Task<List<PromotionNearMissDto>> ApplyPromotionsToSessionItemsAsync(
        SaleSession session,
        string currentUser,
        CancellationToken cancellationToken)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();
        if (activeItems.Count == 0)
            return new List<PromotionNearMissDto>();

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
                return result.NearMissPromotions;

            bool anyChanged = false;

            for (int i = 0; i < activeItems.Count && i < result.CartItems.Count; i++)
            {
                var saleItem = activeItems[i];
                var promoItem = result.CartItems[i];

                // Ricalcola SEMPRE il contributo promozionale, anche a zero — non solo quando aumenta.
                var newPromotionDiscount = promoItem.AppliedPromotions.Count > 0
                    ? promoItem.EffectiveDiscountPercentage
                    : 0m;

                if (newPromotionDiscount != saleItem.PromotionDiscountPercent)
                {
                    saleItem.PromotionDiscountPercent = newPromotionDiscount;
                    saleItem.DiscountPercent = saleItem.ManualDiscountPercent + saleItem.PromotionDiscountPercent;
                    saleItem.PromotionId = promoItem.AppliedPromotions.Count > 0
                        ? promoItem.AppliedPromotions[0].PromotionId
                        : null;
                    saleItem.AppliedPromotionsJSON = promoItem.AppliedPromotions.Count > 0
                        ? promotionService.SerializeAppliedPromotionsJson(promoItem.AppliedPromotions)
                        : null;

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

            return result.NearMissPromotions;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to apply promotions to session {SessionId}; items saved without promotion discount",
                session.Id);
            return new List<PromotionNearMissDto>();
        }
    }

}
